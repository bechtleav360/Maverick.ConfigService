using System;
using System.Collections.Generic;

namespace Bechtle.A365.ConfigService.Common.DbObjects
{
    public class ConfigEnvironmentKeyPath
    {
        public List<ConfigEnvironmentKeyPath> Children { get; set; }

        public ConfigEnvironment ConfigEnvironment { get; set; }

        public Guid ConfigEnvironmentId { get; set; }

        public Guid Id { get; set; }

        public ConfigEnvironmentKeyPath Parent { get; set; }

        public Guid? ParentId { get; set; }

        public string Path { get; set; }

        public string FullPath { get; set; }
    }
}