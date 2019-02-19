using System.Security.Cryptography.X509Certificates;

namespace Bechtle.A365.ConfigService.Services
{
    /// <summary>
    ///     Provides certificates for the requested Region
    /// </summary>
    public interface IRegionEncryptionCertProvider
    {
        /// <summary>
        ///     get the appropriate certificate to encrypt the given region with
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        X509Certificate2 ForRegion(string region);
    }
}