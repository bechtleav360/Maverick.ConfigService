namespace Bechtle.A365.ConfigService.Configuration
{
    /// <summary>
    ///     how to execute authentication
    /// </summary>
    public class AuthenticationConfiguration
    {
        /// <inheritdoc cref="KestrelAuthenticationConfiguration"/>
        public KestrelAuthenticationConfiguration Kestrel { get; set; }
    }
}