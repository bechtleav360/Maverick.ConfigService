using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Bechtle.A365.ConfigService.Implementations.Health
{
    /// <summary>
    ///     Readiness probe that waits for the Http-Pipeline to be configured
    /// </summary>
    public class HttpPipelineCheck : SimpleStateHealthCheck
    {
        /// <inheritdoc />
        protected override HealthCheckResult GetHealthyResult() => HealthCheckResult.Healthy("Http-Pipeline is ready");

        /// <inheritdoc />
        protected override HealthCheckResult GetUnhealthyResult() => HealthCheckResult.Unhealthy("Http-Pipeline is not yet configured");
    }
}