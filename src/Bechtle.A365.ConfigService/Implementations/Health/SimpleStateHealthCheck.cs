using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Bechtle.A365.ConfigService.Implementations.Health
{
    /// <inheritdoc />
    public abstract class SimpleStateHealthCheck : IHealthCheck
    {
        public bool IsReady { get; protected set; } = false;

        /// <inheritdoc />
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
            => Task.FromResult(IsReady switch
            {
                true => HealthCheckResult.Healthy(),
                false => HealthCheckResult.Unhealthy("Http-Pipeline is not yet configured")
            });

        /// <summary>
        ///     Creates a new instance of an Unhealthy result.
        ///     Called when <see cref="IsReady"/> is set to false when <see cref="CheckHealthAsync"/> is executed
        /// </summary>
        /// <returns>instance of <see cref="HealthCheckResult"/></returns>
        protected virtual HealthCheckResult GetUnhealthyResult() => HealthCheckResult.Unhealthy();

        /// <summary>
        ///     Set the state of the Http-Pipeline to "Ready"
        /// </summary>
        /// <param name="ready">optional parameter to set status to not-ready</param>
        public void SetReady(bool ready = true) => IsReady = ready;
    }
}