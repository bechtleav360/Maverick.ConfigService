using System;

namespace Bechtle.A365.ConfigService.Common.DbObjects
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    // necessary for lazy-loading
    public class ConfigEnvironmentKey
    {
        public virtual ConfigEnvironment ConfigEnvironment { get; set; }

        public Guid ConfigEnvironmentId { get; set; }

        public string Description { get; set; }

        public Guid Id { get; set; }

        public string Key { get; set; }

        public string Type { get; set; }

        public string Value { get; set; }

        public long Version { get; set; }
    }
}