using System.Collections.Generic;

namespace Bechtle.A365.ConfigService.Common.Compilation
{
    public class StructureCompilationInfo
    {
        public string Name { get; set; }

        public IDictionary<string, string> Keys { get; set; }

        public IDictionary<string, string> Variables { get; set; }
    }
}