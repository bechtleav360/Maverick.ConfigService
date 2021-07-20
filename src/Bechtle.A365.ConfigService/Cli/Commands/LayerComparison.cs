using System.Collections.Generic;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.Cli.Commands
{
    /// <summary>
    ///     Comparison of two Layers, and the list of Actions to get from <see cref="Source"/> to <see cref="Target"/>
    /// </summary>
    public class LayerComparison
    {
        /// <summary>
        ///     Source-Layer used in this Comparison
        /// </summary>
        public LayerIdentifier Source { get; set; }

        /// <summary>
        ///     Target-Layer used in this Comparison
        /// </summary>
        public LayerIdentifier Target { get; set; }

        /// <summary>
        ///     List of Actions required to get from <see cref="Source"/> to <see cref="Target"/>
        /// </summary>
        public List<ConfigKeyAction> RequiredActions { get; set; }
    }
}