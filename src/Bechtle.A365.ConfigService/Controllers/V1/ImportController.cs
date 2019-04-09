using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers.V1
{
    /// <summary>
    ///     import data from a previous export, <see cref="ExportController"/>
    /// </summary>
    [ApiVersion(ApiVersion)]
    [Route(ApiBaseRoute + "import")]
    public class ImportController : ControllerBase
    {
        private readonly V0.ImportController _previousVersion;

        /// <inheritdoc />
        public ImportController(IServiceProvider provider,
                                ILogger<ImportController> logger,
                                V0.ImportController previousVersion)
            : base(provider, logger)
        {
            _previousVersion = previousVersion;
        }

        /// <summary>
        ///     import a previous exported file
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        [HttpPost(Name = ApiVersionFormatted + "ImportConfiguration")]
        public Task<IActionResult> Import(IFormFile file)
            => _previousVersion.Import(file);
    }
}