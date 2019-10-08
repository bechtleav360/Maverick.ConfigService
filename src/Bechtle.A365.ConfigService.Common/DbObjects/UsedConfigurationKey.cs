using System;

namespace Bechtle.A365.ConfigService.Common.DbObjects
{
    public class UsedConfigurationKey
    {
        public Guid Id { get; set; }

        public string Key { get; set; }

        public ProjectedConfiguration ProjectedConfiguration { get; set; }

        public Guid ProjectedConfigurationId { get; set; }
    }
}