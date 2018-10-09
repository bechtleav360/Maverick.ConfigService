namespace Bechtle.A365.ConfigService.Configuration
{
    /// <summary>
    ///     configuration defining the authentication
    /// </summary>
    public class AuthenticationConfiguration
    {
        /// <summary>
        ///     name of the resource in the authoritative identity-server
        /// </summary>
        public string ApiResourceName { get; set; }

        /// <summary>
        ///     secret for the use ApiResource
        /// </summary>
        public string ApiResourceSecret { get; set; }

        /// <summary>
        ///     space-separated list of scopes used to authorize clients through swagger against the authority
        /// </summary>
        public string SwaggerScopes { get; set; }

        /// <summary>
        ///     Client-Id to use for the swagger authentication
        /// </summary>
        public string SwaggerClientId { get; set; }
    }
}