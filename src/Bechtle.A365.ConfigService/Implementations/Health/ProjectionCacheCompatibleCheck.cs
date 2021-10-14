using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Bechtle.A365.ConfigService.Implementations.Health
{
    /// <summary>
    ///     Health-Check that needs to be true for the service to start.
    ///     Managed by <see cref="ProjectionCacheCleanupService" />.
    /// </summary>
    public class ProjectionCacheCompatibleCheck : SimpleStateHealthCheck
    {
        /// <inheritdoc />
        protected override HealthCheckResult GetHealthyResult()
            => HealthCheckResult.Healthy("projection-cache is compatible");

        /// <inheritdoc />
        protected override HealthCheckResult GetUnhealthyResult()
            => HealthCheckResult.Unhealthy("projection-cache is not checked for compatibility yet, or is being removed for re-building");
    }
}
