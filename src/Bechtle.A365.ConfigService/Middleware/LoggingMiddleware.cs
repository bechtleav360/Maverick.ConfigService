using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Bechtle.A365.ConfigService.Middleware
{
    /// <summary>
    ///     Leaner replacement for Bechtle.A365.Logging.NLog.Infrastructure.Middleware.LoggingMiddleware.
    ///     This one doesn't tamper with the response-stream to log everything we send out...
    /// </summary>
    public class LoggingMiddleware
    {
        private static readonly string AuthenticationHeader = "Authorization";
        private static readonly string CorrelationId = "CorrelationId";

        private readonly RequestDelegate _next;

        /// <inheritdoc />
        /// <param name="next"></param>
        public LoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        ///     required method that is called by the framework
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Global
        public async Task Invoke(HttpContext context)
        {
            //Copy AuthenticationHeader to ASP.NET HttpContext item variable
            CopyHeaderPropertiesToLog(context, AuthenticationHeader, () => string.Empty);

            //Copy CorrelationId to ASP.NET HttpContext item variable
            CopyHeaderPropertiesToLog(context, CorrelationId, () => Guid.NewGuid().ToString("N"));

            context.Response.OnStarting(state =>
            {
                ((HttpContext) state).Response.Headers.Add(CorrelationId, (string) context.Items[CorrelationId]);
                return Task.CompletedTask;
            }, context);

            await _next(context);
        }

        private void CopyHeaderPropertiesToLog(HttpContext context, string propertyName, Func<string> defaultValue)
        {
            context.Items[propertyName] = context.Request
                                                 .Headers[propertyName]
                                                 .FirstOrDefault() ??
                                          defaultValue.Invoke();
        }
    }
}