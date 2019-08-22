using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using App.Metrics;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers
{
    // ApiController is used to identify the Controllers for Swagger
    // ApiVersion is used to map the Controllers to a specific version
    // both attributes are Inherited to the Actual Controllers
    /// <summary>
    ///     Base-Functionality for all Controllers
    /// </summary>
    [ApiController]
    public class ControllerBase : Controller
    {
        /// <summary>
        ///     Prefix before all inheritig Controllers
        /// </summary>
        protected const string ApiBaseRoute = "v{version:apiVersion}/";

        /// <summary>
        ///     ILogger instance ¯\_(ツ)_/¯
        /// </summary>
        protected readonly ILogger Logger;

        /// <summary>
        ///     Data-Encryptor / -Decryptor to protect outgoing data
        /// </summary>
        protected IConfigProtector ConfigProtector;

        /// <summary>
        ///     Certificate-Provider for Regions (configured in appsettings.json)
        /// </summary>
        protected IRegionEncryptionCertProvider EncryptionCertProvider;

        /// <summary>
        ///     IServiceProvider for late service retrieval
        /// </summary>
        protected IServiceProvider Provider;

        /// <summary>
        ///     <see cref="IMetrics"/> to record various application-specific metrics
        /// </summary>
        protected IMetrics Metrics;

        /// <inheritdoc />
        /// <param name="provider"></param>
        /// <param name="logger"></param>
        public ControllerBase(IServiceProvider provider, ILogger logger)
        {
            Provider = provider;
            Logger = logger;
            Metrics = Provider.GetRequiredService<IMetrics>();

            if (provider.GetRequiredService<IConfiguration>()
                        .GetSection("Protection:Enabled")
                        .Get<bool>())
            {
                ConfigProtector = Provider.GetRequiredService<IConfigProtector>();
                EncryptionCertProvider = Provider.GetRequiredService<IRegionEncryptionCertProvider>();
            }
        }

        /// <summary>
        ///     encrypt <paramref name="data" /> using the registered certificates for <paramref name="region" />
        /// </summary>
        /// <param name="region"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        protected string Encrypt(string region, string data)
        {
            var cert = EncryptionCertProvider.ForRegion(region);

            if (cert.Equals(default(X509Certificate2)))
            {
                Logger.LogError($"no certificate found to encrypt region '{region}', can't encrypt response");
                return string.Empty;
            }

            return ConfigProtector.EncryptWithPublicKey(data, cert);
        }

        /// <summary>
        ///     log the failed result and return an appropriate <see cref="HttpStatusCode" /> based on <see cref="Common.Result.Code" />
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        protected IActionResult ProviderError(IResult result)
        {
            Logger.LogError($"received error {result.Code:D}({result.Code:G})");

            switch (result.Code)
            {
                case ErrorCode.None:
                case ErrorCode.Undefined:
                    return StatusCode(HttpStatusCode.InternalServerError, result.Message);

                case ErrorCode.InvalidData:
                case ErrorCode.StructureAlreadyExists:
                case ErrorCode.EnvironmentAlreadyExists:
                case ErrorCode.DefaultEnvironmentAlreadyExists:
                    return StatusCode(HttpStatusCode.BadRequest, result.Message);

                case ErrorCode.DbQueryError:
                case ErrorCode.DbUpdateError:
                    return StatusCode(HttpStatusCode.InternalServerError, result.Message);

                case ErrorCode.NotFound:
                    return StatusCode(HttpStatusCode.NotFound, result.Message);

                default:
                    return StatusCode(HttpStatusCode.InternalServerError, result.Message);
            }
        }

        /// <summary>
        ///     return either the result of <paramref name="successor" /> or <see cref="ProviderError" />
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="result"></param>
        /// <param name="successor"></param>
        /// <returns></returns>
        protected IActionResult Result<T>(IResult<T> result, Func<object, IActionResult> successor)
        {
            if (result.IsError)
                return ProviderError(result);

            return successor(result.Data);
        }

        /// <summary>
        ///     return either <see cref="OkResult" /> or <see cref="ProviderError" />
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="result"></param>
        /// <returns></returns>
        protected IActionResult Result<T>(IResult<T> result) => Result(result, Ok);

        /// <summary>
        ///     return either the result of <paramref name="successor" /> or <see cref="ProviderError" />
        /// </summary>
        /// <param name="result"></param>
        /// <param name="successor"></param>
        /// <returns></returns>
        protected IActionResult Result(IResult result, Func<IActionResult> successor)
        {
            if (result.IsError)
                return ProviderError(result);

            return successor();
        }

        /// <summary>
        ///     return either <see cref="OkResult" /> or <see cref="ProviderError" />
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        protected IActionResult Result(IResult result) => Result(result, Ok);

        /// <summary>
        ///     wrapper around <see cref="Microsoft.AspNetCore.Mvc.ControllerBase.StatusCode(int)" />
        /// </summary>
        /// <param name="statusCode"></param>
        /// <returns></returns>
        protected IActionResult StatusCode(HttpStatusCode statusCode) => StatusCode((int) statusCode);

        /// <summary>
        ///     wrapper around <see cref="Microsoft.AspNetCore.Mvc.ControllerBase.StatusCode(int, object)" />
        /// </summary>
        /// <param name="statusCode"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected IActionResult StatusCode(HttpStatusCode statusCode, object value) => StatusCode((int) statusCode, value);
    }
}