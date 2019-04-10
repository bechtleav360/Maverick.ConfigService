using System;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.Objects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.V1.Controllers
{
    /// <summary>
    ///     export data to import it at a later time in a different location
    /// </summary>
    [ApiVersion(ApiVersion)]
    [Route(ApiBaseRoute + "export")]
    public class ExportController : VersionedController<V0.Controllers.ExportController>
    {
        /// <inheritdoc />
        public ExportController(IServiceProvider provider,
                                ILogger<ExportController> logger,
                                V0.Controllers.ExportController previousVersion)
            : base(provider, logger, previousVersion)
        {
        }

        /// <summary>
        ///     export one or more items
        /// </summary>
        /// <param name="definition"></param>
        /// <returns></returns>
        [HttpPost(Name = ApiVersionFormatted + "ExportConfiguration")]
        public Task<IActionResult> Export([FromBody] ExportDefinition definition)
            => PreviousVersion.Export(definition);
    }
}