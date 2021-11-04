using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.Objects;
using Bechtle.A365.ConfigService.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers.V1
{
    /// <summary>
    ///     export data to import it at a later time in a different location
    /// </summary>
    [Route(ApiBaseRoute + "export")]
    public class ExportController : ControllerBase
    {
        private readonly IDataExporter _exporter;

        /// <inheritdoc />
        public ExportController(
            ILogger<ExportController> logger,
            IDataExporter exporter)
            : base(logger)
        {
            _exporter = exporter;
        }

        /// <summary>
        ///     export one or more items
        /// </summary>
        /// <param name="definition"></param>
        /// <returns></returns>
        [HttpPost(Name = "ExportConfiguration")]
        [ApiVersion(ApiVersions.V1, Deprecated = ApiDeprecation.V1)]
        [ApiVersion(ApiVersions.V11, Deprecated = ApiDeprecation.V11)]
        public async Task<IActionResult> Export([FromBody] ExportDefinition? definition)
        {
            if (definition is null)
                return BadRequest("no definition received");

            if (!definition.Environments.Any() && !definition.Layers.Any())
                return BadRequest("no Environments or Layers given to export");

            try
            {
                var result = await _exporter.Export(definition);

                if (result.IsError)
                    return ProviderError(result);

                return File(
                    new MemoryStream(
                        Encoding.UTF8.GetBytes(
                            JsonSerializer.Serialize(result.Data, new JsonSerializerOptions { WriteIndented = true }))),
                    "application/octet-stream",
                    "export.json");
            }
            catch (Exception e)
            {
                string targetEnvironments = string.Join(", ", definition.Environments.Select(eid => $"{eid.Category}/{eid.Name}"));
                string targetLayers = string.Join(", ", definition.Layers.Select(l => l.Name));

                KnownMetrics.Exception.WithLabels(e.GetType().Name).Inc();
                Logger.LogError(
                    e,
                    "failed to export environments ({TargetEnvironments}), and layers ({TargetLayers})",
                    targetEnvironments,
                    targetLayers);
                return StatusCode(
                    HttpStatusCode.InternalServerError,
                    $"failed to export environments ({targetEnvironments}), and layers ({targetLayers})");
            }
        }
    }
}
