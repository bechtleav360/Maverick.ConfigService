using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.V1.Controllers
{
    /// <summary>
    ///     import data from a previous export, <see cref="ExportController"/>
    /// </summary>
    [ApiVersion(ApiVersion)]
    [Route(ApiBaseRoute + "import")]
    public class ImportController : VersionedController<V0.Controllers.ImportController>
    {
        /// <inheritdoc />
        public ImportController(IServiceProvider provider,
                                ILogger<ImportController> logger,
                                V0.Controllers.ImportController previousVersion)
            : base(provider, logger, previousVersion)
        {
        }

        /// <summary>
        ///     import a previous exported file
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        [HttpPost(Name = ApiVersionFormatted + "ImportConfiguration")]
        public Task<IActionResult> Import(IFormFile file)
            => PreviousVersion.Import(file);
    }
}