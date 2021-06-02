using System;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bechtle.A365.ConfigService.Controllers.V1
{
    /// <summary>
    ///     read information on how to connect to this Service
    /// </summary>
    [Route(ApiBaseRoute + "connections")]
    [ApiVersion(ApiVersions.V1, Deprecated = ApiDeprecation.V1)]
    public class ConnectionController : ControllerBase
    {
        private readonly IOptionsMonitor<EventBusConnectionConfiguration> _config;

        /// <inheritdoc />
        public ConnectionController(ILogger<ConnectionController> logger,
                                    IOptionsMonitor<EventBusConnectionConfiguration> config)
            : base(logger)
        {
            _config = config;
        }

        /// <summary>
        ///     get information on how to Connect to the used EventBus-Server and -Hub
        /// </summary>
        /// <returns></returns>
        [HttpGet("events", Name = "GetEventConnection")]
        public IActionResult GetEventConnection()
        {
            KnownMetrics.ConnectionInfo.Inc();

            HttpContext.Response.OnStarting(state =>
            {
                if (state is HttpContext context)
                {
                    context.Response.Headers.Add("X-EventBus-Server", _config.CurrentValue.Server);
                    context.Response.Headers.Add("X-EventBus-Hub", _config.CurrentValue.Hub);
                }

                return Task.CompletedTask;
            }, HttpContext);

            return NoContent();
        }
    }
}