using System.Collections.Generic;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    public class StreamedEnvironmentKeyPath
    {
        public string FullPath { get; set; } = string.Empty;

        public string Path { get; set; } = string.Empty;

        public List<StreamedEnvironmentKeyPath> Children { get; set; } = new List<StreamedEnvironmentKeyPath>();

        public StreamedEnvironmentKeyPath Parent { get; set; } = null;
    }
}