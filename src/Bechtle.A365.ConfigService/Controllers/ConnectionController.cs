﻿using System;
using System.Threading.Tasks;
using App.Metrics;
using Bechtle.A365.ConfigService.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers
{
    /// <summary>
    ///     read information on how to connect to this Service
    /// </summary>
    [Route(ApiBaseRoute + "connections")]
    [ApiVersion(ApiVersions.V1, Deprecated = ApiDeprecation.V1)]
    public class ConnectionController : ControllerBase
    {
        private readonly EventBusConnectionConfiguration _config;
        private readonly IMetrics _metrics;

        /// <inheritdoc />
        public ConnectionController(IServiceProvider provider,
                                    ILogger<ConnectionController> logger,
                                    EventBusConnectionConfiguration config,
                                    IMetrics metrics)
            : base(provider, logger)
        {
            _config = config;
            _metrics = metrics;
        }

        /// <summary>
        ///     get information on how to Connect to the used EventBus-Server and -Hub
        /// </summary>
        /// <returns></returns>
        [HttpGet("events", Name = "GetEventConnection")]
        public IActionResult GetEventConnection()
        {
            _metrics.Measure.Counter.Increment(KnownMetrics.ConnectionInfo);

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