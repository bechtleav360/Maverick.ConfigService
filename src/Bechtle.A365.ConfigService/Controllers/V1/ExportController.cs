using System;
using Bechtle.A365.ConfigService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers.V1
{
    /// <summary>
    ///     export data to import it at a later time in a different location
    /// </summary>
    [ApiVersion(ApiVersion)]
    [Route(ApiBaseRoute + "export")]
    public class ExportController : V0.ExportController
    {
        private new const string ApiVersion = "1.0";

        /// <inheritdoc />
        public ExportController(IServiceProvider provider,
                                ILogger<ExportController> logger,
                                IDataExporter exporter)
            : base(provider, logger, exporter)
        {
        }
    }
}