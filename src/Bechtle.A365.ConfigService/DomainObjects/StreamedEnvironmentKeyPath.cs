using System.Collections.Generic;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     Node inside a Tree of Paths to represent Environment-Data
    /// </summary>
    public class StreamedEnvironmentKeyPath
    {
        /// <summary>
        ///     Full Path including Parents
        /// </summary>
        public string FullPath { get; set; } = string.Empty;

        /// <summary>
        ///     last Path-Component of this Node
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        ///     List of Children that may come after this
        /// </summary>
        public List<StreamedEnvironmentKeyPath> Children { get; set; } = new List<StreamedEnvironmentKeyPath>();

        /// <summary>
        ///     Reference to Parent-Node
        /// </summary>
        public StreamedEnvironmentKeyPath Parent { get; set; } = null;
    }
}