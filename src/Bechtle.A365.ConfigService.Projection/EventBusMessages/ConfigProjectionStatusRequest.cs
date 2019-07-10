using Bechtle.A365.Core.EventBus.Events.Abstraction;

namespace Bechtle.A365.ConfigService.Projection.EventBusMessages
{
    public class ConfigProjectionStatusRequest : IA365Event
    {
        public string EventName => nameof(ConfigProjectionStatusRequest);
    }
}