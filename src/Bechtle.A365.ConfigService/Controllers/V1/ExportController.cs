using System;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.Objects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers.V1
{
    /// <summary>
    ///     export data to import it at a later time in a different location
    /// </summary>
    [ApiVersion(ApiVersion)]
    [Route(ApiBaseRoute + "export")]
    public class ExportController : ControllerBase
    {
        private new const string ApiVersion = "1.0";
        private readonly V0.ExportController _previousVersion;

        /// <inheritdoc />
        public ExportController(IServiceProvider provider,
                                ILogger<ExportController> logger,
                                V0.ExportController previousVersion)
            : base(provider, logger)
        {
            _previousVersion = previousVersion;
        }

        /// <summary>
        ///     export one or more items
        /// </summary>
        /// <param name="definition"></param>
        /// <returns></returns>
        [HttpPost(Name = ApiVersionFormatted + "ExportConfiguration")]
        public Task<IActionResult> Export([FromBody] ExportDefinition definition)
            => _previousVersion.Export(definition);
    }
}