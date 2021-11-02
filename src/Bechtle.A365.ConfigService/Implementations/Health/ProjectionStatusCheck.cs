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
        private static readonly object _statusLock = new();
        private static bool CurrentlyProjecting;
        private static ProjectionStatus? CurrentStatus;
        private static StreamHead? CurrentStreamHead;

        /// <summary>
        ///     Shows if the internal projection has, at one point, caught up to the current stream-head.
        ///     If it fell behind at any point after that (other instance writes new events),
        ///     it's still considered "caught-up"
        /// </summary>
        private static bool HasCaughtUp;

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
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = new())
        {
            ProjectionStatus copiedStatus;
            StreamHead copiedHead;

            lock (_statusLock)
            {
                copiedStatus = new ProjectionStatus(
                    CurrentStatus?.EventId ?? Guid.Empty,
                    CurrentStatus?.EventType ?? string.Empty,
                    CurrentStatus?.EventNumber ?? 0,
                    CurrentStatus?.StartedAt ?? DateTime.UnixEpoch,
                    CurrentStatus?.FinishedAt ?? DateTime.UnixEpoch,
                    CurrentStatus?.Error ?? string.Empty
                );

                copiedHead = new StreamHead(
                    CurrentStreamHead?.EventId ?? Guid.Empty,
                    CurrentStreamHead?.EventType ?? string.Empty,
                    CurrentStreamHead?.EventNumber ?? 0);
            }

            var projectionStatusInfo = new Dictionary<string, object?>
            {
                { "HeadEventId", copiedHead.EventId },
                { "HeadEventType", copiedHead.EventType },
                { "HeadEventNumber", copiedHead.EventNumber },
                { "EventId", copiedStatus.EventId },
                { "EventType", copiedStatus.EventType },
                { "EventNumber", copiedStatus.EventNumber },
                { "StartedAt", copiedStatus.StartedAt },
                { "FinishedAt", copiedStatus.FinishedAt },
                { "Error", copiedStatus.Error },
            };

            if (HasCaughtUp)
            {
                return Task.FromResult(
                    HealthCheckResult.Healthy(
                        data: projectionStatusInfo));
            }

            return Task.FromResult(
                HealthCheckResult.Unhealthy(
                    "projection has not caught up to stream-head yet.",
                    data: projectionStatusInfo));
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

                // set to true when possible
                // don't reset this flag - see CurrentStreamHead for more info
                if (CurrentStreamHead?.EventId == CurrentStatus?.EventId)
                {
                    HasCaughtUp = true;
                }

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
                if (CurrentStatus?.EventId == eventHeader.EventId)
                {
                    CurrentStatus = new ProjectionStatus(
                        eventHeader.EventId,
                        eventHeader.EventType,
                        eventHeader.EventNumber,
                        CurrentStatus?.StartedAt ?? DateTime.UnixEpoch,
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
                if (CurrentStatus?.EventId == eventHeader.EventId)
                {
                    CurrentStatus = new ProjectionStatus(
                        eventHeader.EventId,
                        eventHeader.EventType,
                        eventHeader.EventNumber,
                        CurrentStatus?.StartedAt ?? DateTime.UnixEpoch,
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
        ///     Sets the currently latest event in the configured Stream
        /// </summary>
        /// <param name="eventHeader">metadata for the last event in the stream</param>
        public void SetHeadEvent(StreamedEventHeader eventHeader)
        {
            lock (_statusLock)
            {
                CurrentStreamHead = new StreamHead(
                    eventHeader.EventId,
                    eventHeader.EventType,
                    eventHeader.EventNumber);

                // this is for the case when:
                // 1. the service starts with an up-to-date cache
                // 2. the projection starts at the head-event (CurrentStatus)
                // 3. the Head is set to the same event (CurrentStreamHead)
                // usually 2. and 3. are switched because the service is catching up
                if (CurrentStreamHead?.EventId == CurrentStatus?.EventId)
                {
                    HasCaughtUp = true;
                }

                _logger.LogDebug(
                    "set head to event {EventId:D}#{EventNumber}-{EventType}",
                    eventHeader.EventId,
                    eventHeader.EventNumber,
                    eventHeader.EventType);
            }
        }

        /// <summary>
        ///     actual status-data with <see cref="HealthCheckResult" />
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

        /// <summary>
        ///     information for the current head-event in the configured config-stream
        /// </summary>
        private readonly struct StreamHead
        {
            public readonly Guid EventId;
            public readonly string EventType;
            public readonly ulong EventNumber;

            public StreamHead(
                Guid eventId,
                string eventType,
                ulong eventNumber)
            {
                EventId = eventId;
                EventType = eventType;
                EventNumber = eventNumber;
            }
        }
    }
}
