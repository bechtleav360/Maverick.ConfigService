using System;

namespace Bechtle.A365.ConfigService.Common.DbObjects
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    // necessary for lazy-loading
    public class UsedConfigurationKey
    {
        public Guid ProjectedConfigurationId { get; set; }

        public virtual ProjectedConfiguration ProjectedConfiguration { get; set; }

        public Guid Id { get; set; }

        public string Key { get; set; }
    }
}