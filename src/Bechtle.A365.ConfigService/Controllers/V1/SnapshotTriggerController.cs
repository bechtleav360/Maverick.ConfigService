using System;
using System.Net;
using Bechtle.A365.ConfigService.Implementations.SnapshotTriggers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers.V1
{
    /// <summary>
    ///     Controller to trigger On-Demand Snapshots
    /// </summary>
    [Route(ApiBaseRoute + "snapshots")]
    [ApiVersion(ApiVersions.V1, Deprecated = ApiDeprecation.V1)]
    public class SnapshotTriggerController : ControllerBase
    {
        /// <inheritdoc />
        public SnapshotTriggerController(IServiceProvider provider,
                                         ILogger<SnapshotTriggerController> logger)
            : base(provider, logger)
        {
        }

        /// <summary>
        ///     Trigger a new Snapshot
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public IActionResult TriggerSnapshot()
        {
            try
            {
                OnDemandSnapshotTrigger.TriggerOnDemandSnapshot();
                return Accepted();
            }
            catch (Exception e)
            {
                Logger.LogWarning(e, "error while triggering On-Demand snapshot");
                return StatusCode(HttpStatusCode.InternalServerError);
            }
        }
    }
}