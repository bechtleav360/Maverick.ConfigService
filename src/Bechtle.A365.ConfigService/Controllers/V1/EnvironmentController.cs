using System;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers.V1
{
    /// <summary>
    /// </summary>
    [ApiVersion(ApiVersion)]
    [Route(ApiBaseRoute + "environments")]
    public class EnvironmentController : V0.EnvironmentController
    {
        private new const string ApiVersion = "1.0";

        /// <inheritdoc />
        public EnvironmentController(IServiceProvider provider,
                                     ILogger<EnvironmentController> logger,
                                     IProjectionStore store,
                                     IEventStore eventStore,
                                     IJsonTranslator translator)
            : base(provider, logger, store, eventStore, translator)
        {
        }
    }
}