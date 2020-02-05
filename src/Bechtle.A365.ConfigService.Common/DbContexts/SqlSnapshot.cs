using System;

namespace Bechtle.A365.ConfigService.Common.DbContexts
{
    public class SqlSnapshot
    {
        public string DataType { get; set; }

        public Guid Id { get; set; }

        public string Identifier { get; set; }

        public string JsonData { get; set; }

        public long MetaVersion { get; set; }

        public long Version { get; set; }
    }
}