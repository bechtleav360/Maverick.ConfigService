using System;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers.V1
{
    /// <summary>
    ///     read existing or create new Config-Structures
    /// </summary>
    [ApiVersion(ApiVersion)]
    [Route(ApiBaseRoute + "structures")]
    public class StructureController : V0.StructureController
    {
        private new const string ApiVersion = "1.0";

        /// <inheritdoc />
        public StructureController(IServiceProvider provider,
                                   ILogger<StructureController> logger,
                                   IProjectionStore store,
                                   IEventStore eventStore,
                                   IJsonTranslator translator)
            : base(provider, logger, store, eventStore, translator)
        {
        }
    }
}