using System;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Projection.EventBusMessages;
using Bechtle.A365.Core.EventBus.Events.Messages;

namespace Bechtle.A365.ConfigService.Projection.Services
{
    public class StatusReporter : HostedService
    {
        private readonly IMetricService _metricService;
        private readonly IEventBusService _eventBus;

        public StatusReporter(IServiceProvider serviceProvider,
                              IMetricService metricService,
                              IEventBusService eventBus)
            : base(serviceProvider)
        {
            _metricService = metricService;
            _eventBus = eventBus;
            _metricService.StatusChanged += OnMetricsChanged;
        }

        private void OnMetricsChanged(object sender, EventArgs e) => PublishStatus().RunSync();

        public Task PublishStatus() => _eventBus.Publish(new EventMessage
        {
            EventName = nameof(ConfigProjectionStatusResponse),
            Event = new ConfigProjectionStatusResponse {NodeStatus = _metricService.GetCurrentStatus()}
        });

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();

            await _eventBus.Subscribe<ConfigProjectionStatusRequest>(OnStatusRequest);
        }

        private void OnStatusRequest(string sender, EventMessage message) => PublishStatus().RunSync();
    }
}