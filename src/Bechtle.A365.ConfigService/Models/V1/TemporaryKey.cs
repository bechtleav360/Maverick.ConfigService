namespace Bechtle.A365.ConfigService.Models.V1
{
    /// <summary>
    ///     describes a temporary key that should be stored for later use
    /// </summary>
    public class TemporaryKey
    {
        /// <summary>
        ///     Name / Key for this temporary entry
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        ///     Value stored in this temporary entry
        /// </summary>
        public string? Value { get; set; }
    }
}
