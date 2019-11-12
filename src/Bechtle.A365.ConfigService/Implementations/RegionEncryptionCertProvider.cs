using System.IO;
using System.Security.Cryptography.X509Certificates;
using Bechtle.A365.ConfigService.Configuration;
using Bechtle.A365.ConfigService.Interfaces;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Implementations
{
    /// <inheritdoc />
    public class RegionEncryptionCertProvider : IRegionEncryptionCertProvider
    {
        private readonly ILogger<RegionEncryptionCertProvider> _logger;
        private readonly ProtectedConfiguration _protectedConfiguration;

        /// <inheritdoc />
        public RegionEncryptionCertProvider(ProtectedConfiguration protectedConfiguration,
                                            ILogger<RegionEncryptionCertProvider> logger)
        {
            _logger = logger;
            _protectedConfiguration = protectedConfiguration;
        }

        /// <inheritdoc />
        public X509Certificate2 ForRegion(string region)
        {
            if (_protectedConfiguration.Regions.ContainsKey(region))
            {
                _logger.LogDebug($"using certificate '{"ServiceCerts/" + _protectedConfiguration.Regions[region]}' for region '{region}'; using '{region}'");
                return new X509Certificate2("ServiceCerts/" + _protectedConfiguration.Regions[region]);
            }

            if (_protectedConfiguration.Regions.ContainsKey("*"))
            {
                var autoCert = "ServiceCerts/" + _protectedConfiguration.Regions["*"].Replace("*", region);

                if (File.Exists(autoCert))
                {
                    _logger.LogDebug($"using certificate '{autoCert}' for region '{region}'; using '*'");
                    return new X509Certificate2(autoCert);
                }

                _logger.LogWarning($"certificate '{autoCert}' not found for region '{region}'; using '*'");
            }

            _logger.LogWarning($"no certificate found for region '{region}'");

            return default;
        }
    }
}