using Bechtle.A365.ConfigService.Projection.Models;
using Bechtle.A365.Core.EventBus.Events.Abstraction;

namespace Bechtle.A365.ConfigService.Projection.EventBusMessages
{
    public class ConfigProjectionStatusResponse : IA365Event
    {
        public string EventName => nameof(ConfigProjectionStatusResponse);

        public ProjectionNodeStatus NodeStatus { get; set; }
    }
}