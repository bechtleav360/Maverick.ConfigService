namespace Bechtle.A365.ConfigService.Dto
{
    /// <summary>
    ///     transfer-object defining a Configuration-Key with additional metadata
    /// </summary>
    public class DtoConfigKey
    {
        /// <summary>
        ///     uniquely identifying key
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        ///     associated value of this key
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        ///     short description of this keys value
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        ///     intended type of this keys value
        /// </summary>
        public string Type { get; set; }
    }
}