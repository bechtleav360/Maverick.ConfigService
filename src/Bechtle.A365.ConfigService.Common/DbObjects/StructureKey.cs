using System;

namespace Bechtle.A365.ConfigService.Common.DbObjects
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    // necessary for lazy-loading
    public class StructureKey
    {
        public Guid Id { get; set; }

        public string Key { get; set; }

        public Guid StructureId { get; set; }

        public virtual Structure Structure { get; set; }

        public string Value { get; set; }
    }
}