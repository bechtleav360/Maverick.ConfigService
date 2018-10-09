using System;
using Bechtle.A365.ConfigService.Configuration;

namespace Bechtle.A365.ConfigService.Extensions
{
    public static class EndpointConfigurationExtension
    {
        public static Uri ToUri(this EndpointConfiguration endpoint) => new Uri(ToUriString(endpoint));

        public static string ToUriString(this EndpointConfiguration endpoint)
            => $@"{endpoint.Protocol}://{endpoint.Address}:{endpoint.Port}{endpoint.RootPath}";

        public static bool IsValid(this EndpointConfiguration endpoint) => !string.IsNullOrWhiteSpace(endpoint.Name) &&
                                                                           Uri.TryCreate(ToUriString(endpoint), UriKind.Absolute, out _);
    }
}