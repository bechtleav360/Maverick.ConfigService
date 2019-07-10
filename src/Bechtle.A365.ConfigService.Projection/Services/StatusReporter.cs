using System;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Projection.EventBusMessages;
using Bechtle.A365.Core.EventBus.Abstraction;
using Bechtle.A365.Core.EventBus.Events.Messages;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Projection.Services
{
    public class StatusReporter : HostedService
    {
        private readonly ILogger _logger;
        private readonly ProjectionMetricService _metricService;
        private readonly IEventBus _eventBus;

        public StatusReporter(IServiceProvider serviceProvider,
                              ILogger<StatusReporter> logger,
                              ProjectionMetricService metricService,
                              IEventBus eventBus)
            : base(serviceProvider)
        {
            _logger = logger;
            _metricService = metricService;
            _eventBus = eventBus;
        }

        public Task PublishStatus() => _eventBus.Publish(new EventMessage
        {
            EventName = nameof(ConfigProjectionStatusResponse),
            Event = new ConfigProjectionStatusResponse {NodeStatus = _metricService.GetCurrentStatus()}
        });

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();

            await _eventBus.Connect();

            await _eventBus.Subscribe<ConfigProjectionStatusRequest>(OnStatusRequest);
        }

        private void OnStatusRequest(string sender, EventMessage message) => PublishStatus().RunSync();
    }
}