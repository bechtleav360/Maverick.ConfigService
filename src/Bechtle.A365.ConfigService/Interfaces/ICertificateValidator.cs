using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Authentication.Certificates.Events;

namespace Bechtle.A365.ConfigService.Interfaces
{
    /// <summary>
    ///     Custom-Certificate-Validator.
    ///     Validate Certificate against Custom-Rules in <see cref="Validate(ValidateCertificateContext)" />,
    ///     and execute <see cref="Fail(CertificateAuthenticationFailedContext)" /> in case it fails to Validate
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