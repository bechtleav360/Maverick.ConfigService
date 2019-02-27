using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.Common.Objects
{
    /// <summary>
    ///     definition of what should be exported
    /// </summary>
    public class ExportDefinition
    {
        /// <summary>
        ///     list of environments that should be exported
        /// </summary>
        public EnvironmentIdentifier[] Environments { get; set; }
    }
}