namespace Bechtle.A365.ConfigService.Configuration
{
    /// <summary>
    ///     configuration regarding the cert-auth in Kestrel
    /// </summary>
    public class KestrelAuthenticationConfiguration
    {
        /// <summary>
        ///     path to the local certificate to use
        /// </summary>
        public string Certificate { get; set; }

        /// <summary>
        ///     configure kestrel to use cert-based authentication
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        ///     IpAddress to bind to, use "::" or "0.0.0.0" for any ip
        /// </summary>
        public string IpAddress { get; set; }

        /// <summary>
        ///     password for <see cref="Certificate"/>, use null to indicate no password
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        ///     port to bind kestrel to
        /// </summary>
        public int Port { get; set; }
    }
}