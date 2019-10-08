using System;

namespace Bechtle.A365.ConfigService.Common.DbObjects
{
    public class StructureVariable
    {
        public Guid Id { get; set; }

        public string Key { get; set; }

        public Structure Structure { get; set; }

        public Guid StructureId { get; set; }

        public string Value { get; set; }
    }
}