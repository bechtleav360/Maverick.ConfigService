using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.V1.Controllers
{
    /// <summary>
    ///     read information on how to connect to this Service
    /// </summary>
    [ApiVersion(ApiVersion)]
    [Route(ApiBaseRoute + "connections")]
    public class ConnectionController : VersionedController<V0.Controllers.ConnectionController>
    {
        /// <inheritdoc />
        public ConnectionController(IServiceProvider provider,
                                    ILogger<ConnectionController> logger,
                                    V0.Controllers.ConnectionController previousVersion)
            : base(provider, logger, previousVersion)
        {
        }

        /// <summary>
        ///     get information on how to Connect to the used EventBus-Server and -Hub
        /// </summary>
        /// <returns></returns>
        [HttpGet("events", Name = ApiVersionFormatted + "GetEventConnection")]
        public IActionResult GetEventConnection()
            => PreviousVersion.GetEventConnection();
    }
}