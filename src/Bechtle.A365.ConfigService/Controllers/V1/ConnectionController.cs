using System;
using Bechtle.A365.ConfigService.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers.V1
{
    /// <summary>
    ///     read information on how to connect to this Service
    /// </summary>
    [ApiVersion(ApiVersion)]
    [Route(ApiBaseRoute + "connections")]
    public class ConnectionController : V0.ConnectionController
    {
        private new const string ApiVersion = "1.0";

        /// <inheritdoc />
        public ConnectionController(IServiceProvider provider,
                                    ILogger<ConnectionController> logger,
                                    EventBusConnectionConfiguration config)
            : base(provider, logger, config)
        {
        }
    }
}