using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Bechtle.A365.ConfigService.Middleware
{
    /// <summary>
    ///     redirect v0-requests to their appropriate v1 counterpart
    /// </summary>
    public class V0RedirectMiddleware
    {
        private readonly RequestDelegate _next;

        /// <inheritdoc />
        public V0RedirectMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        ///     actual implementation of this middleware
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/v1"))
                return _next(context);

            context.Response.Redirect("/v1" + context.Request.Path.Value, true, true);
            return Task.CompletedTask;
        }
    }
}