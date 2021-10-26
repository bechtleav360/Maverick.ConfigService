using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ServiceBase.EventStore.Abstractions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Implementations.Health
{
    /// <summary>
    ///     Status-Check that represents the current State of the underlying DomainEvent-Projection
    /// </summary>
    public class ProjectionStatusCheck : IHealthCheck
    {
        private static readonly object _statusLock = new object();
        private static bool CurrentlyProjecting;
        private static ProjectionStatus CurrentStatus;

        private readonly ILogger<ProjectionStatusCheck> _logger;

        /// <summary>
        ///     Create a new instance of <see cref="ProjectionStatusCheck" />
        /// </summary>
        /// <param name="logger">logger to write diagnostic information to</param>
        public ProjectionStatusCheck(ILogger<ProjectionStatusCheck> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
        {
            ProjectionStatus copiedStatus;

            lock (_statusLock)
            {
                copiedStatus = new ProjectionStatus(
                    CurrentStatus.EventId,
                    CurrentStatus.EventType,
                    CurrentStatus.EventNumber,
                    CurrentStatus.StartedAt,
                    CurrentStatus.FinishedAt,
                    CurrentStatus.Error
                );
            }

            return Task.FromResult(
                HealthCheckResult.Healthy(
                    data: new Dictionary<string, object?>
                    {
                        { "EventId", copiedStatus.EventId },
                        { "EventType", copiedStatus.EventType },
                        { "EventNumber", copiedStatus.EventNumber },
                        { "StartedAt", copiedStatus.StartedAt },
                        { "FinishedAt", copiedStatus.FinishedAt },
                        { "Error", copiedStatus.Error }
                    }));
        }

        /// <summary>
        ///     Checks if the Projection is currently processing any events
        /// </summary>
        /// <returns>true if the projection is currently processing an event, otherwise false</returns>
        public bool IsCurrentlyProjecting() => CurrentlyProjecting;

        /// <summary>
        ///     Set the status to 'currently processing event'
        /// </summary>
        /// <param name="eventHeader">event that is now being processed</param>
        public void SetCurrentlyProjecting(StreamedEventHeader eventHeader)
        {
            lock (_statusLock)
            {
                CurrentStatus = new ProjectionStatus(
                    eventHeader.EventId,
                    eventHeader.EventType,
                    eventHeader.EventNumber,
                    DateTime.UtcNow,
                    null,
                    string.Empty);

                CurrentlyProjecting = true;

                _logger.LogDebug(
                    "set status to currently processing event {EventId:D}#{EventNumber}-{EventType}",
                    eventHeader.EventId,
                    eventHeader.EventNumber,
                    eventHeader.EventType);
            }
        }

        /// <summary>
        ///     Set the status to 'done processing event'
        /// </summary>
        /// <param name="eventHeader">event that was just projected</param>
        public void SetDoneProjecting(StreamedEventHeader eventHeader)
        {
            lock (_statusLock)
            {
                if (CurrentStatus.EventId == eventHeader.EventId)
                {
                    CurrentStatus = new ProjectionStatus(
                        eventHeader.EventId,
                        eventHeader.EventType,
                        eventHeader.EventNumber,
                        CurrentStatus.StartedAt,
                        DateTime.UtcNow,
                        string.Empty);

                    CurrentlyProjecting = false;

                    _logger.LogDebug(
                        "set status to done processing event {EventId:D}#{EventNumber}-{EventType}",
                        eventHeader.EventId,
                        eventHeader.EventNumber,
                        eventHeader.EventType);
                }
            }
        }

        /// <summary>
        ///     Set the status to 'failed while processing event'
        /// </summary>
        /// <param name="eventHeader">event that could not be properly processed</param>
        /// <param name="exception">error that prevented proper processing</param>
        public void SetErrorWhileProjecting(StreamedEventHeader eventHeader, Exception exception)
        {
            lock (_statusLock)
            {
                if (CurrentStatus.EventId == eventHeader.EventId)
                {
                    CurrentStatus = new ProjectionStatus(
                        eventHeader.EventId,
                        eventHeader.EventType,
                        eventHeader.EventNumber,
                        CurrentStatus.StartedAt,
                        DateTime.UtcNow,
                        exception.Message);

                    CurrentlyProjecting = false;

                    _logger.LogDebug(
                        "set status to failed while processing event {EventId:D}#{EventNumber}-{EventType}",
                        eventHeader.EventId,
                        eventHeader.EventNumber,
                        eventHeader.EventType);
                }
            }
        }

        /// <summary>
        ///     actual status-data with <see cref="HealthCheckResult"/>
        /// </summary>
        private readonly struct ProjectionStatus
        {
            public readonly Guid EventId;
            public readonly string EventType;
            public readonly ulong EventNumber;
            public readonly DateTime StartedAt;
            public readonly DateTime? FinishedAt;
            public readonly string Error;

            public ProjectionStatus(
                Guid eventId,
                string eventType,
                ulong eventNumber,
                DateTime startedAt,
                DateTime? finishedAt,
                string error)
            {
                EventId = eventId;
                EventType = eventType;
                EventNumber = eventNumber;
                StartedAt = startedAt;
                FinishedAt = finishedAt;
                Error = error;
            }
        }
    }
}
