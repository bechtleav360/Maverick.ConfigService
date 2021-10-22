namespace Bechtle.A365.ConfigService.Common.Objects
{
    /// <summary>
    ///     transfer-object defining a Configuration-Key with additional metadata
    /// </summary>
    public record DtoConfigKey
    {
        /// <summary>
        ///     short description of this keys value
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        ///     uniquely identifying key
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        ///     intended type of this keys value
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        ///     associated value of this key
        /// </summary>
        public string? Value { get; set; }
    }
}
