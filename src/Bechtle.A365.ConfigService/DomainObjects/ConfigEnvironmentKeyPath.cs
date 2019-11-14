using System.Collections.Generic;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     Node inside a Tree of Paths to represent Environment-Data
    /// </summary>
    public class ConfigEnvironmentKeyPath
    {
        /// <summary>
        ///     List of Children that may come after this
        /// </summary>
        public List<ConfigEnvironmentKeyPath> Children { get; set; } = new List<ConfigEnvironmentKeyPath>();

        /// <summary>
        ///     Full Path including Parents
        /// </summary>
        public string FullPath { get; set; } = string.Empty;

        /// <summary>
        ///     Reference to Parent-Node
        /// </summary>
        public ConfigEnvironmentKeyPath Parent { get; set; } = null;

        /// <summary>
        ///     last Path-Component of this Node
        /// </summary>
        public string Path { get; set; } = string.Empty;
    }
}