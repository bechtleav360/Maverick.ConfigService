using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Bechtle.A365.ConfigService.Implementations.Health
{
    /// <summary>
    ///     Checks the current Status of <see cref="DomainObjectProjection"/>
    /// </summary>
    public class DomainEventProjectionCheck : SimpleStateHealthCheck
    {
        /// <inheritdoc />
        protected override HealthCheckResult GetUnhealthyResult()
            => new(
                HealthStatus.Unhealthy,
                "DomainEventProjection is not connected to EventStore");
    }
}
