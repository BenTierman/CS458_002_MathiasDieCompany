﻿using FeatureRecognitionAPI.Models.Enums;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;

namespace FeatureRecognitionAPI.Models.Dtos
{
    public class FeatureDto
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public PossibleFeatureTypes FeatureType { get; set; }

        public List<EntityDto> EntityList { get; set; }
        public int Count { get; set; }
        public double Perimiter { get; set; }
        public bool MultipleRadius { get; set; }
        public bool KissCut { get; set; }
    }
}
