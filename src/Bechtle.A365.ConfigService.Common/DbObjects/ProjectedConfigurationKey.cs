using System;

namespace Bechtle.A365.ConfigService.Common.DbObjects
{
    public class ProjectedConfigurationKey
    {
        public Guid Id { get; set; }

        public string Key { get; set; }

        public ProjectedConfiguration ProjectedConfiguration { get; set; }

        public Guid ProjectedConfigurationId { get; set; }

        public string Value { get; set; }
    }
}