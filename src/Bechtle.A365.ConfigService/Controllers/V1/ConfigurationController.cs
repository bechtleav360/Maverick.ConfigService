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
    [Route(ApiBaseRoute + "configurations")]
    public class ConfigurationController : V0.ConfigurationController
    {
        private new const string ApiVersion = "1.0";

        /// <inheritdoc />
        public ConfigurationController(IServiceProvider provider,
                                       ILogger<ConfigurationController> logger,
                                       IEventStore eventStore,
                                       IProjectionStore store,
                                       IJsonTranslator translator)
            : base(provider, logger, eventStore, store, translator)
        {
        }
    }
}