﻿using Newtonsoft.Json.Linq;

namespace Bechtle.A365.ConfigService.Dto
{
    public class DtoStructure
    {
        public string Name { get; set; }

        public int Version { get; set; }

        public JToken Structure { get; set; }
    }
}