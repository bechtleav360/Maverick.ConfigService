using System.Collections.Generic;

namespace Bechtle.A365.ConfigService.Common.Compilation
{
    public class EnvironmentCompilationInfo
    {
        public IDictionary<string, string> Keys { get; set; }

        public string Name { get; set; }
    }
}