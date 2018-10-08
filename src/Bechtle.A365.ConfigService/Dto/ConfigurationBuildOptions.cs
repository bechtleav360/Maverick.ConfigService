using System;

namespace Bechtle.A365.ConfigService.Dto
{
    /// <summary>
    ///     Options regarding how a Configuration is Built and how it is available after that
    /// </summary>
    public class ConfigurationBuildOptions
    {
        /// <summary>
        ///     Available from this point in time, or 'always available' if null
        /// </summary>
        public DateTime? ValidFrom { get; set; }

        /// <summary>
        ///     Available up to this point in time, or indefinitely if null
        /// </summary>
        public DateTime? ValidTo { get; set; }
    }
}