using System;
using Bechtle.A365.ConfigService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers.V1
{
    /// <summary>
    ///     import data from a previous export, <see cref="ExportController"/>
    /// </summary>
    [ApiVersion(ApiVersion)]
    [Route(ApiBaseRoute + "import")]
    public class ImportController : V0.ImportController
    {
        private new const string ApiVersion = "1.0";

        /// <inheritdoc />
        public ImportController(IServiceProvider provider,
                                ILogger<ImportController> logger,
                                IDataImporter importer)
            : base(provider, logger, importer)
        {
        }
    }
}