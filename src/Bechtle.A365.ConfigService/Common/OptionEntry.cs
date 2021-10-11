namespace Bechtle.A365.ConfigService.Common
{
    /// <summary>
    ///     a single entry returned from [EventStore]/options
    /// </summary>
    public class OptionEntry
    {
        /// <summary>
        ///     Name of the Option
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        ///     Value of the Option
        /// </summary>
        public string Value { get; set; } = string.Empty;
    }
}
