using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Authentication.Certificates.Events;

namespace Bechtle.A365.ConfigService.Services
{
    /// <summary>
    /// </summary>
    public interface ICertificateValidator
    {
        /// <summary>
        ///     delegate to execute after validation has failed
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        Task Fail(CertificateAuthenticationFailedContext context);

        /// <summary>
        ///     validate certificate of incoming request
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        Task Validate(ValidateCertificateContext context);
    }
}