using System;
using System.Collections.Generic;

namespace Bechtle.A365.ConfigService.Common.DbObjects
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    // necessary for lazy-loading
    public class ProjectedConfiguration
    {
        public Guid ConfigEnvironmentId { get; set; }

        public virtual ConfigEnvironment ConfigEnvironment { get; set; }

        public Guid Id { get; set; }

        public virtual List<ProjectedConfigurationKey> Keys { get; set; }

        public Guid StructureId { get; set; }

        public virtual Structure Structure { get; set; }

        public int StructureVersion { get; set; }

        public string ConfigurationJson { get; set; }

        public DateTime? ValidFrom { get; set; }

        public DateTime? ValidTo { get; set; }
    }
}