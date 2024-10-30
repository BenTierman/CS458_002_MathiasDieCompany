﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FeatureRecognitionAPI.Models;

namespace Testing_for_Project
{
    internal class ExtendLineTests
    {
        [Test]
        public void ExtendPerpendicularLines() {
            Line line1 = new(7,4,7,6);
            Line line2 = new(7, 8, 7, 10);
            List<Entity> testEntities = [line1, line2];
            Feature testFeature = new(testEntities, false, false);
            testFeature.extendAllEntities();
            Line finalTestLine = new(7, 4, 7, 10);
            Assert.IsTrue(testFeature.ExtendedEntityList.Count == 1);
            Assert.IsTrue(testFeature.ExtendedEntityList[0] is Line);
            Assert.IsTrue(((Line)testFeature.ExtendedEntityList[0]) == finalTestLine);
        }
    }
}
