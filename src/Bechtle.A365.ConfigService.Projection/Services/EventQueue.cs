using System;
using System.Collections.Concurrent;
using App.Metrics;
using Bechtle.A365.ConfigService.Projection.Metrics;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Projection.Services
{
    public class EventQueue : IEventQueue
    {
        private readonly ConcurrentQueue<ProjectedEvent> _queuedEvents;
        private readonly ILogger _logger;
        private readonly IMetrics _metrics;
        private readonly IMetricService _metricService;

        public EventQueue(ILogger<EventQueue> logger,
                          IMetrics metrics,
                          IMetricService metricService)
        {
            _queuedEvents = new ConcurrentQueue<ProjectedEvent>();
            _logger = logger;
            _metrics = metrics;
            _metricService = metricService;
        }

        public bool TryEnqueue(ProjectedEvent projectedEvent)
        {
            try
            {
                _queuedEvents.Enqueue(projectedEvent);

                _metricService.SetQueueLength(_queuedEvents.Count)
                              .Finish();

                _metrics.Measure.Counter.Increment(KnownMetrics.QueueLength);

                _logger.LogInformation($"event added to queue; " +
                                       $"{_queuedEvents.Count} events remaining; " +
                                       $"'{projectedEvent.DomainEvent.EventType}' / '{projectedEvent.Id}'");

                return true;
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "could not push DomainEvent into queue");
                return false;
            }
        }

        public bool TryDequeue(out ProjectedEvent projectedEvent)
        {
            try
            {
                if (!_queuedEvents.TryDequeue(out projectedEvent)) 
                    return false;

                _metricService.SetQueueLength(_queuedEvents.Count)
                              .Finish();

                _metrics.Measure.Counter.Decrement(KnownMetrics.QueueLength);

                _logger.LogInformation($"event dequeued; " +
                                       $"{_queuedEvents.Count} events remaining; " +
                                       $"'{projectedEvent.DomainEvent.EventType}' / '{projectedEvent.Id}'");
                return true;

            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "could not dequeue DomainEvent from queue");
                projectedEvent = default;
                return false;
            }
        }
    }
}