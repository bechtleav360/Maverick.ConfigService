using System;
using System.Collections.Generic;

namespace Bechtle.A365.ConfigService.Common.DbObjects
{
    public class ProjectedConfiguration
    {
        public ConfigEnvironment ConfigEnvironment { get; set; }

        public Guid ConfigEnvironmentId { get; set; }

        public string ConfigurationJson { get; set; }

        public bool UpToDate { get; set; }

        public Guid Id { get; set; }

        public List<ProjectedConfigurationKey> Keys { get; set; }

        public Structure Structure { get; set; }

        public Guid StructureId { get; set; }

        public int StructureVersion { get; set; }

        public List<UsedConfigurationKey> UsedConfigurationKeys { get; set; }

        public DateTime? ValidFrom { get; set; }

        public DateTime? ValidTo { get; set; }

        public long Version { get; set; }
    }
}