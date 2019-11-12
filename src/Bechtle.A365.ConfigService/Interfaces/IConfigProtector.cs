using System.Security.Cryptography.X509Certificates;

namespace Bechtle.A365.ConfigService.Interfaces
{
    /// <summary>
    ///     encrypt / decrypt data for transfer between clients
    /// </summary>
    public interface IConfigProtector
    {
        /// <summary>
        ///     decrypts data using the private key of the given certificate
        /// </summary>
        /// <param name="data"></param>
        /// <param name="cert"></param>
        /// <returns></returns>
        string DecryptWithPrivateKey(string data, X509Certificate2 cert);

        /// <summary>
        ///     decrypts data using the public key of the given certificate
        /// </summary>
        /// <param name="data"></param>
        /// <param name="cert"></param>
        /// <returns></returns>
        string DecryptWithPublicKey(string data, X509Certificate2 cert);

        /// <summary>
        ///     encrypts data using the private key of the given certificate
        /// </summary>
        /// <param name="data"></param>
        /// <param name="cert"></param>
        /// <returns></returns>
        string EncryptWithPrivateKey(string data, X509Certificate2 cert);

        /// <summary>
        ///     encrypts data using the public key of the given certificate
        /// </summary>
        /// <param name="data"></param>
        /// <param name="cert"></param>
        /// <returns></returns>
        string EncryptWithPublicKey(string data, X509Certificate2 cert);
    }
}