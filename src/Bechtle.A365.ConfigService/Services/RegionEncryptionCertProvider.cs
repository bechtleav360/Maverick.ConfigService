using System.IO;
using System.Security.Cryptography.X509Certificates;
using Bechtle.A365.ConfigService.Configuration;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Services
{
    /// <inheritdoc />
    public class RegionEncryptionCertProvider : IRegionEncryptionCertProvider
    {
        private readonly ILogger<RegionEncryptionCertProvider> _logger;
        private ProtectedConfiguration ProtectedConfiguration { get; set; }

        /// <inheritdoc />
        public RegionEncryptionCertProvider(ProtectedConfiguration protectedConfiguration,
                                            ILogger<RegionEncryptionCertProvider> logger)
        {
            _logger = logger;
            ProtectedConfiguration = protectedConfiguration;
        }

        /// <inheritdoc />
        public X509Certificate2 ForRegion(string region)
        {
            if (ProtectedConfiguration.Regions.ContainsKey(region))
            {
                _logger.LogDebug($"using certificate '{"ServiceCerts/" + ProtectedConfiguration.Regions[region]}' for region '{region}'; using '{region}'");
                return new X509Certificate2("ServiceCerts/" + ProtectedConfiguration.Regions[region]);
            }

            if (ProtectedConfiguration.Regions.ContainsKey("*"))
            {
                var autoCert = "ServiceCerts/" + ProtectedConfiguration.Regions["*"].Replace("*", region);

                if (File.Exists(autoCert))
                {
                    _logger.LogDebug($"using certificate '{autoCert}' for region '{region}'; using '*'");
                    return new X509Certificate2(autoCert);
                }

                _logger.LogWarning($"certificate '{autoCert}' not found for region '{region}'; using '*'");
            }

            _logger.LogWarning($"no certificate found for region '{region}'");

            return default(X509Certificate2);
        }
    }
}