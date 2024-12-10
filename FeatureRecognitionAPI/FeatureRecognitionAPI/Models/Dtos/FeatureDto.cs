﻿using FeatureRecognitionAPI.Models.Enums;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;

namespace FeatureRecognitionAPI.Models.Dtos
{
    public class Features
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public PossibleFeatureTypes FeatureType { get; set; }

        public List<EntityDto>? EntityList { get; set; } // At least some properties will not be null, depending on the entity type
        public int Count { get; set; }
        public double Perimeter { get; set; }
        public bool MultipleRadius { get; set; }
        public bool KissCut { get; set; }
    }

    public class FeatureDto
    {
        public required List<Features> Features { get; set; }
        public int Count { get; set; } // Number Up 

    }
}
