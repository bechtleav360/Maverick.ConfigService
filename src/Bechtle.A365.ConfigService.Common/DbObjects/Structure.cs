using System;
using System.Collections.Generic;

namespace Bechtle.A365.ConfigService.Common.DbObjects
{
    public class Structure
    {
        public Guid Id { get; set; }

        public List<StructureKey> Keys { get; set; }

        public string Name { get; set; }

        public List<StructureVariable> Variables { get; set; }

        public int Version { get; set; }
    }
}