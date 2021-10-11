using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.Objects;
using Bechtle.A365.ConfigService.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers.V1
{
    /// <summary>
    ///     import data from a previous export, <see cref="ExportController" />
    /// </summary>
    [Route(ApiBaseRoute + "import")]
    public class ImportController : ControllerBase
    {
        private readonly IDataImporter _importer;

        /// <inheritdoc />
        public ImportController(
            ILogger<ImportController> logger,
            IDataImporter importer)
            : base(logger)
        {
            _importer = importer;
        }

        /// <summary>
        ///     import a previous exported file
        /// </summary>
        /// <param name="file"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost(Name = "ImportConfiguration")]
        [ApiVersion(ApiVersions.V1, Deprecated = ApiDeprecation.V1)]
        [ApiVersion(ApiVersions.V11, Deprecated = ApiDeprecation.V11)]
        public async Task<IActionResult> Import(IFormFile? file, CancellationToken cancellationToken)
        {
            if (file is null || file.Length == 0)
                return BadRequest("no or empty file uploaded");

            byte[] buffer;

            await using (var memStream = new MemoryStream())
            {
                await file.CopyToAsync(memStream, cancellationToken);
                memStream.Seek(0, SeekOrigin.Begin);
                buffer = new byte[memStream.Length];
                await memStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
            }

            if (!buffer.Any())
                return BadRequest("no or empty file uploaded");

            var json = Encoding.UTF8.GetString(buffer);

            ConfigExport export;

            // try to strip utf8-byte-order-mark from the incoming text
            var bom = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
            if (json.StartsWith(bom))
                json = json.TrimStart(bom.ToCharArray());

            try
            {
                export = JsonSerializer.Deserialize<ConfigExport>(json);

                if (export is null)
                    return BadRequest("uploaded file can't be mapped to object");
            }
            catch (JsonException e)
            {
                Logger.LogWarning($"uploaded file can't be deserialized to '{nameof(ConfigExport)}': {e}");
                return BadRequest("uploaded file can't be mapped to object");
            }

            try
            {
                var result = await _importer.Import(export, cancellationToken);

                if (result.IsError)
                    return ProviderError(result);

                return AcceptedAtAction(
                    nameof(EnvironmentController.GetEnvironments),
                    RouteUtilities.ControllerName<EnvironmentController>(),
                    new { version = ApiVersions.V1 });
            }
            catch (Exception e)
            {
                string targetEnvironments = string.Join(
                    ", ",
                    export.Environments.Select(eid => $"{eid.Category}/{eid.Name}"));

                KnownMetrics.Exception.WithLabels(e.GetType().Name).Inc();
                Logger.LogError(e, $"failed to import '{export.Environments.Length}' environments ({targetEnvironments})");
                return StatusCode(HttpStatusCode.InternalServerError, $"failed to import environments '{targetEnvironments}'");
            }
        }
    }
}
