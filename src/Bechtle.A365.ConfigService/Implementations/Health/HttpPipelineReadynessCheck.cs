﻿using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Bechtle.A365.ConfigService.Implementations.Health
{
    /// <summary>
    ///     Readyness probe that waits for the Http-Pipeline to be configured
    /// </summary>
    public class HttpPipelineReadinessCheck : SimpleStateHealthCheck
    {
        /// <inheritdoc />
        protected override HealthCheckResult GetUnhealthyResult() => HealthCheckResult.Unhealthy("Http-Pipeline is not yet configured");
    }
}