using System.IO;
using System.Security.Cryptography.X509Certificates;
using Bechtle.A365.ConfigService.Configuration;
using Bechtle.A365.ConfigService.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bechtle.A365.ConfigService.Implementations
{
    /// <inheritdoc />
    public class RegionEncryptionCertProvider : IRegionEncryptionCertProvider
    {
        private readonly ILogger<RegionEncryptionCertProvider> _logger;
        private readonly IOptionsMonitor<ProtectedConfiguration> _configuration;

        /// <inheritdoc cref="RegionEncryptionCertProvider" />
        public RegionEncryptionCertProvider(IOptionsMonitor<ProtectedConfiguration> configuration,
                                            ILogger<RegionEncryptionCertProvider> logger)
        {
            _logger = logger;
            _configuration = configuration;
        }

        /// <inheritdoc />
        public X509Certificate2 ForRegion(string region)
        {
            var config = _configuration.CurrentValue;

            if (config.Regions.ContainsKey(region))
            {
                _logger.LogDebug($"using certificate '{"ServiceCerts/" + config.Regions[region]}' for region '{region}'; using '{region}'");
                return new X509Certificate2("ServiceCerts/" + config.Regions[region]);
            }

            if (config.Regions.ContainsKey("*"))
            {
                var autoCert = "ServiceCerts/" + config.Regions["*"].Replace("*", region);

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