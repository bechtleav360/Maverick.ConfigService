using System;
using System.Net;
using Bechtle.A365.ConfigService.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers
{
    public class ControllerBase : Controller
    {
        /// <summary>
        /// </summary>
        protected const string ApiBaseRoute = "";

        /// <summary>
        /// </summary>
        protected readonly ILogger Logger;

        /// <summary>
        ///     IServiceProvider for late service retrieval
        /// </summary>
        protected IServiceProvider Provider;

        /// <summary>
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="logger"></param>
        public ControllerBase(IServiceProvider provider, ILogger logger)
        {
            Provider = provider;
            Logger = logger;
        }

        /// <summary>
        ///     log the failed result and return an appropriate <see cref="HttpStatusCode" /> based on <see cref="Common.Result.Code" />
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        protected IActionResult ProviderError(Result result)
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
        ///     return either the result of <paramref name="successor"/> or <see cref="ProviderError"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="result"></param>
        /// <param name="successor"></param>
        /// <returns></returns>
        protected IActionResult Result<T>(Result<T> result, Func<object, IActionResult> successor)
        {
            if (result.IsError)
                return ProviderError(result);

            return successor(result.Data);
        }

        /// <summary>
        ///     return either <see cref="OkResult"/> or <see cref="ProviderError"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="result"></param>
        /// <returns></returns>
        protected IActionResult Result<T>(Result<T> result) => Result(result, Ok);

        /// <summary>
        ///     return either the result of <paramref name="successor"/> or <see cref="ProviderError"/>
        /// </summary>
        /// <param name="result"></param>
        /// <param name="successor"></param>
        /// <returns></returns>
        protected IActionResult Result(Result result, Func<IActionResult> successor)
        {
            if (result.IsError)
                return ProviderError(result);

            return successor();
        }

        /// <summary>
        ///     return either <see cref="OkResult"/> or <see cref="ProviderError"/>
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        protected IActionResult Result(Result result) => Result(result, Ok);

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