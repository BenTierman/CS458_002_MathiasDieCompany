﻿/*
 * For borders look at every feature that is not inside another feature (that is a border)
 * calc number up using number of borders
 * 
 * for optimization before detecting features ignore all entity groups outside
 * the first border and calculates feautrues only for that one 
 */
using FeatureRecognitionAPI.Models;
using iText.Layout.Splitting;
using iText.StyledXmlParser.Node;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.IO;
using System.Numerics;

public class Feature
{
    [JsonProperty]
    PossibleFeatureTypes featureType;
    [JsonProperty]
    List<Entity> entityList; //list of touching entities that make up the feature
    [JsonProperty]
    bool kissCut;
    [JsonProperty]
    bool multipleRadius;
    [JsonProperty]
    double perimeter;
    [JsonProperty]
    bool border;
    public int count;
    [Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]

    protected List<Entity> extendedEntityList; // list of entities after extending them all
    protected List<Entity> baseEntityList; // what the list is sorted into from extendedEntityList which should only
                                           // contain entities that make up the base shape and possibly corner features
    protected List<List<Entity>> PerimeterEntityList; // 2 dimensional list where each list at each index is a group of
                                                      // touching entities that make up a single perimeter feature for
                                                      // the original feature
    //EXAMPLE: <[list for Mitiered notch], [list for raduis notch], [list for Group17], [list for chamfered corner]>
    // You will have to run detection for perimeter features for each index of this list
    protected enum PossibleFeatureTypes
    {
        [JsonProperty]
        Punch,
        Group1A1,
        Group1A2,
        Group1B1,
        Group1B2,
        Group3,
        Group1C,
        Group6,
        Group2A
    }

    private Feature() { }//should not use default constructor

    public Feature(string featureType, bool kissCut, bool multipleRadius, bool border)
    {
        this.count = 1;
        //change string input to enum value
        PossibleFeatureTypes inputAsEnum = (PossibleFeatureTypes)Enum.Parse(typeof(PossibleFeatureTypes), featureType);
        this.featureType = inputAsEnum;
        this.kissCut = kissCut;
        this.multipleRadius = multipleRadius;
        this.border = border;

        calcPerimeter();
    }

    public Feature(List<Entity> entityList)
    {
        this.count = 1;
        this.entityList = entityList;

        int numLines = 0;
        int numArcs = 0;
        int numCircles = 0;

        //count the number of each entity type
        for (int i = 0; i < entityList.Count; i++)
        {
            if (entityList[i] is Line)
            {
                numLines++;
            }
            else if (entityList[i] is Arc)
            {
                numArcs++;
            }
            else if (entityList[i] is Circle)
            {
                numCircles++;
            }
            else
            {
                Console.WriteLine("Error: Cannot detect entity type.");
                break;
            }
        }

        //check two conditions possible to make Group1B (with no perimeter features)
        if (numCircles == 1 || (numLines == 2 && numArcs == 2))
        {
            if (numCircles == 1 && numLines == 0 && numArcs == 0)
            {
                Circle c = entityList[0] as Circle;
                if (c.radius <= 1.625)
                {
                    featureType = PossibleFeatureTypes.Punch;
                }
                else featureType = PossibleFeatureTypes.Group1B1;
            }
            else featureType = PossibleFeatureTypes.Group1B2;
        }
        //check two conditions possible to make Group1A (with no perimeter features)
        else if (numLines == 4)
        {
            if (numArcs == 0)
            {
                featureType = PossibleFeatureTypes.Group1A1;
            }
            else featureType = PossibleFeatureTypes.Group1A2;
        }
        else
        {
            Console.WriteLine("Error: Cannot assign feature type.");
        }

        //calculate and set the perimeter of the feature
        calcPerimeter();
    }

    //calculates the perimeter or diameter of the feature
    public void calcPerimeter()
    {
        if (featureType == PossibleFeatureTypes.Punch || featureType == PossibleFeatureTypes.Group1B1)
        {
            perimeter = entityList[0].getLength() / Math.PI;
        }

        else
        {
            for (int i = 0; i < entityList.Count; i++)
            {
                perimeter += entityList[i].getLength();
            }
        }
    }

    /*
     * Overriding the Equals method to compare two Feature objects
    */
    public override bool Equals(object obj)
    {
        var item = obj as Feature;
        if (item == null)
        {
            return false;
        }

        //calculate difference in order to use tolerence
        double perDiff = perimeter - item.perimeter;

        //if the features are identical Group1B features
        if (featureType == item.featureType && featureType == PossibleFeatureTypes.Group1B1 && kissCut == item.kissCut && multipleRadius == item.multipleRadius &&
                Math.Abs(perDiff) < 0.0005 && border == item.border)
        {
                //serialize and deserialize in order to set them to circle objects, should look into different way of doing this
                //TODO: create added check if they are arcs instead of circles
                var serializedParent = JsonConvert.SerializeObject(entityList[0]);
                Circle c1 = JsonConvert.DeserializeObject<Circle>(serializedParent);
                serializedParent = JsonConvert.SerializeObject(item.entityList[0]);
                Circle c2 = JsonConvert.DeserializeObject<Circle>(serializedParent);

                if (c1.radius == c2.radius)
                {
                    return true;
                }
        }

        // Checking equality of all other feature types
        else if (featureType == item.featureType && kissCut == item.kissCut && multipleRadius == item.multipleRadius &&
                Math.Abs(perDiff) < 0.0005 && border == item.border)
        {
            return true;
        }
        //not equal
        return false;
    }

    public void extendAllEntities(List<Entity> myEntityList)
    {
        for (int i = 0; i < myEntityList.Count; i++)
        {
            if (myEntityList[i] is Line)
            {
                for (int j = 0; j < myEntityList.Count; j++)
                {
                    if (myEntityList[j] is Line)
                    {
                        if (!extendTwoLines((Line)myEntityList[i], (Line)myEntityList[j]))
                        {
                            if (!extendedEntityList.Contains(myEntityList[i]))
                            {
                                extendedEntityList.Add(myEntityList[i]);
                            }
                            if (!extendedEntityList.Contains(myEntityList[j]))
                            {
                                extendedEntityList.Add(myEntityList[j]);
                            }
                        }
                    }
                }
            }
        }
    }

    //Method that takes two lines and extends them to touch if they are:
    // 1. not already touching
    // 2. are parallel or perpendicular
    //adds extended line(parallel) or lines(perpendicular) to extendedEntityList
    //returns true if lines were extended, otherwise false
    public bool extendTwoLines(Line line1, Line line2)
    {
        if (!line1.DoesIntersect(line2))
        //makes sure youre not extending lines that already touch
        {
            //check if the lines are parallel or perpendicular
            if (line1.isPerpendicular(line2))
            {
                Point intersectPoint = line1.getIntersectPoint(line1, line2);
                Point PointToExtendLine1 = line1.findPointToExtend(line1, intersectPoint);
                Point PointToExtendLine2 = line2.findPointToExtend(line2, intersectPoint);
                if (PointToExtendLine1 != null && PointToExtendLine2 != null)
                {
                    // Logic for extending line1: determines what point to extend for line1
                    if (PointToExtendLine1.X == line1.StartX && PointToExtendLine1.Y == line1.StartY)
                    {
                        //make new line1 line with extended start point
                        extendedEntityList.Add(new Line(intersectPoint.X, intersectPoint.Y, line1.EndX, line1.EndY, true));
                    }
                    else
                    {
                        //make new line1 line with extended end point
                        extendedEntityList.Add(new Line(line1.StartX, line1.StartY, intersectPoint.X, intersectPoint.Y, true));
                    }


                    // Logic for extending line2: determines what point to extend for line2
                    if (PointToExtendLine2.X == line2.StartX && PointToExtendLine2.Y == line2.StartY)
                    {
                        //make new line2 line with extended start point
                        extendedEntityList.Add(new Line(intersectPoint.X, intersectPoint.Y, line2.EndX, line2.EndY, true));
                    }
                    else
                    {
                        //make new line2 line with extended end point
                        extendedEntityList.Add(new Line(line2.StartX, line2.StartY, intersectPoint.X, intersectPoint.Y, true));
                    }
                    return true;
                }
            }
            else if (line1.isParallel(line2))
            {
                Point pointToExtend;
                Line tempLine = new Line(true);//makes a new line object with extendedLine boolean to true
                if (line1.findDistance(
                    new Point(line1.StartX, line1.StartY),
                    new Point(line2.StartX, line2.StartY))
                    < line1.findDistance(
                    new Point(line1.EndX, line1.EndY),
                    new Point(line2.StartX, line2.StartY)))
                //This looks like a lot but all this is doing is finding the closest point on line1 to line2
                {
                    //At this point we know the point to be extended on line1 is the start point, meaning the end point can stay the same
                    //  Hence why tempLine end point is set to line1's
                    pointToExtend = new Point(line1.StartX, line1.StartY);
                    tempLine.StartX = line1.EndX;
                    tempLine.StartY = line1.EndY;
                }
                else
                {
                    pointToExtend = new Point(line1.EndX, line1.EndY);
                    tempLine.StartX = line1.StartX;
                    tempLine.StartY = line1.StartY;
                }
                if (line2.findDistance(
                    pointToExtend,
                    new Point(line2.StartX, line2.StartY))
                    > line2.findDistance(
                    pointToExtend,
                    new Point(line2.EndX, line2.EndY)))
                //Similar to the one above but finds what point on line2 is farthest from line1's point to extend
                {
                    tempLine.EndX = line2.StartX;
                    tempLine.EndY = line2.StartY;
                }
                else
                {
                    tempLine.EndX = line2.EndX;
                    tempLine.EndY = line2.EndY;
                }
                extendedEntityList.Add(tempLine);
                return true;//extended a parallel lines into 1
            }
        }
        return false;
    }
}