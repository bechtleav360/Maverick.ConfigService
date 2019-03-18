using System;
using Bechtle.A365.ConfigService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers.V1
{
    /// <summary>
    ///     retrieve the history of various objects
    /// </summary>
    [ApiVersion(ApiVersion)]
    [Route(ApiBaseRoute + "history")]
    public class HistoryController : V0.HistoryController
    {
        private new const string ApiVersion = "1.0";

        /// <inheritdoc />
        public HistoryController(IServiceProvider provider,
                                 ILogger<HistoryController> logger,
                                 IEventStore eventStore)
            : base(provider, logger, eventStore)
        {
        }
    }
}