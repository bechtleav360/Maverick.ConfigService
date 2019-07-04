using Bechtle.A365.Core.EventBus.Events.Events.Base;

namespace Bechtle.A365.ConfigService.Common.Events
{
    /// <summary>
    ///     sent when a new Configuration has been built
    /// </summary>
    public class OnConfigurationPublished : EventBase
    {
        /// <summary>
        ///     Environment Category (Tenant / Customer / ...)
        /// </summary>
        public string EnvironmentCategory { get; set; }

        /// <summary>
        ///     Environment Name (Dev / Prod / ...)
        /// </summary>
        public string EnvironmentName { get; set; }

        /// <summary>
        ///     Structure Name (Consumer / Service / App)
        /// </summary>
        public string StructureName { get; set; }

        /// <summary>
        ///     Structure Version
        /// </summary>
        public int StructureVersion { get; set; }
    }
}