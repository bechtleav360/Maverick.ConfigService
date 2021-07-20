namespace Bechtle.A365.ConfigService.Common.Converters
{
    /// <summary>
    ///     Default-Settings used during the translation between Map/Json
    /// </summary>
    public static class JsonTranslatorDefaultSettings
    {
        /// <summary>
        ///     Default Path-Separator used when no other is set
        /// </summary>
        public static readonly string Separator = "/";

        /// <summary>
        ///     Default value for if Paths should be escaped
        /// </summary>
        public static readonly bool EscapePath = false;
    }
}