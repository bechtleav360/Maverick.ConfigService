namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     All Metadata used to represent a single Key inside a <see cref="StreamedEnvironment" />
    /// </summary>
    public class StreamedEnvironmentKey
    {
        /// <summary>
        ///     Description of the Contents of this Key
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        ///     Full-Path for this Key
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        ///     Intended Value-Type of this Key
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        ///     String-Representation of the actual Value
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        ///     internal data-version
        /// </summary>
        public long Version { get; set; }
    }
}