using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.Objects;
using Bechtle.A365.ConfigService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Bechtle.A365.ConfigService.Controllers.NonVersioned
{
    /// <summary>
    ///     import data from a previous export, <see cref="ExportController"/>
    /// </summary>
    [Route("import")]
    public class ImportController : ControllerBase
    {
        private readonly IDataImporter _importer;

        /// <inheritdoc />
        public ImportController(IServiceProvider provider,
                                ILogger<ImportController> logger,
                                IDataImporter importer)
            : base(provider, logger)
        {
            _importer = importer;
        }

        /// <summary>
        ///     import a previous exported file
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        [ApiVersion(ApiVersions.V0)]
        [HttpPost(Name = "Deprecated_Fallback_ImportConfiguration")]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file is null || file.Length == 0)
                return BadRequest("no or empty file uploaded");

            byte[] buffer;

            using (var memStream = new MemoryStream())
            {
                await file.CopyToAsync(memStream);
                memStream.Seek(0, SeekOrigin.Begin);
                buffer = new byte[memStream.Length];
                memStream.Read(buffer, 0, buffer.Length);
            }

            if (!buffer.Any())
                return BadRequest("no or empty file uploaded");

            var json = Encoding.UTF8.GetString(buffer);

            ConfigExport export;

            try
            {
                export = JsonConvert.DeserializeObject<ConfigExport>(json);

                if (export is null)
                    return BadRequest("uploaded file can't be mapped to object");
            }
            catch (JsonException e)
            {
                Logger.LogWarning($"uploaded file can't be deserialized to '{nameof(ConfigExport)}': {e}");
                return BadRequest("uploaded file can't be mapped to object");
            }

            var result = await _importer.Import(export);

            if (result.IsError)
                return ProviderError(result);

            return Ok(result);
        }
    }
}