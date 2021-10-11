using System;
using System.Net;
using Bechtle.A365.ConfigService.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers.V1
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
        ///     Prefix before all inherited Controllers
        /// </summary>
        protected const string ApiBaseRoute = "v{version:apiVersion}/";

        /// <summary>
        ///     ILogger instance ¯\_(ツ)_/¯
        /// </summary>
        protected readonly ILogger Logger;

        /// <inheritdoc />
        /// <param name="logger"></param>
        public ControllerBase(ILogger logger)
        {
            Logger = logger;
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
                case ErrorCode.ValidationFailed:
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
        protected IActionResult Result<T>(IResult<T> result, Func<object?, IActionResult> successor)
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
