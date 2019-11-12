using System.Security.Claims;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Authentication.Certificates.Events;
using Bechtle.A365.ConfigService.Interfaces;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Implementations
{
    /// <inheritdoc />
    public class CertificateValidator : ICertificateValidator
    {
        private readonly ILogger _logger;

        /// <inheritdoc />
        public CertificateValidator(ILogger<CertificateValidator> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public Task Fail(CertificateAuthenticationFailedContext context)
        {
            if (_logger.IsEnabled(LogLevel.Trace))
                _logger.LogTrace("certificate failed to validate: \r\n" +
                                 $"AuthenticationScheme: {context.Scheme}\r\n" +
                                 $"Result: {context.Result}\r\n" +
                                 $"Exception: {context.Exception}");

            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug($"certificate failed to validate: {context.Exception}");

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task Validate(ValidateCertificateContext context)
        {
            if (!context.ClientCertificate.Issuer.EndsWith("DC=A365DEV, DC=DE"))
            {
                context.Fail("certificate does not belong to A365 domain");
            }
            else
            {
                context.Principal = new ClaimsPrincipal(
                    new ClaimsIdentity(
                        new[]
                        {
                            new Claim(ClaimTypes.NameIdentifier,
                                      context.ClientCertificate.Subject,
                                      ClaimValueTypes.String,
                                      context.Options.ClaimsIssuer),

                            new Claim(ClaimTypes.Name,
                                      context.ClientCertificate.Subject,
                                      ClaimValueTypes.String,
                                      context.Options.ClaimsIssuer)
                        },
                        context.Scheme.Name));

                context.Success();
            }

            return Task.CompletedTask;
        }
    }
}