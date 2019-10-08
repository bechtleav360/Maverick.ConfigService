using System;
using System.Collections.Generic;

namespace Bechtle.A365.ConfigService.Common.DbObjects
{
    public class ConfigEnvironment
    {
        public string Category { get; set; }

        public bool DefaultEnvironment { get; set; }

        public Guid Id { get; set; }

        public List<ConfigEnvironmentKey> Keys { get; set; }

        public string Name { get; set; }
    }
}