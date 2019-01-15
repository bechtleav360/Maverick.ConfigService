using System;
using System.Collections.Generic;

namespace Bechtle.A365.ConfigService.Common.DbObjects
{
    public class ConfigEnvironmentKeyPath
    {
        public virtual List<ConfigEnvironmentKeyPath> Children { get; set; }

        public virtual ConfigEnvironment ConfigEnvironment { get; set; }

        public Guid ConfigEnvironmentId { get; set; }

        public Guid Id { get; set; }

        public virtual ConfigEnvironmentKeyPath Parent { get; set; }

        public Guid? ParentId { get; set; }

        public string Path { get; set; }

        public string FullPath { get; set; }
    }
}