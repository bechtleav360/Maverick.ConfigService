using System;
using System.Collections.Generic;

namespace Bechtle.A365.ConfigService.Dto.DbObjects
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    // necessary for lazy-loading
    public class Structure
    {
        public Guid Id { get; set; }

        public virtual List<StructureKey> Keys { get; set; }

        public string Name { get; set; }

        public int Version { get; set; }
    }
}