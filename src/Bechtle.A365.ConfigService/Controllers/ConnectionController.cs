using System;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers
{
    [Route(ApiBaseRoute + "connections")]
    public class ConnectionController : ControllerBase
    {
        private readonly EventBusConnectionConfiguration _config;

        /// <inheritdoc />
        public ConnectionController(IServiceProvider provider,
                                    ILogger<ConnectionController> logger,
                                    EventBusConnectionConfiguration config)
            : base(provider, logger)
        {
            _config = config;
        }

        [HttpGet("events")]
        public IActionResult GetEventConnection()
        {
            HttpContext.Response.OnStarting(state =>
            {
                if (state is HttpContext context)
                {
                    context.Response.Headers.Add("X-EventBus-Server", _config.Server);
                    context.Response.Headers.Add("X-EventBus-Hub", _config.Hub);
                }

                return Task.CompletedTask;
            }, HttpContext);

            return NoContent();
        }
    }
}