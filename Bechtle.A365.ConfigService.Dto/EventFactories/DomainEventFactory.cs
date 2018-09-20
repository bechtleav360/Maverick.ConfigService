using System;
using Bechtle.A365.ConfigService.Dto.DomainEvents;

namespace Bechtle.A365.ConfigService.Dto.EventFactories
{
    public static class DomainEventFactory
    {
        public static (byte[] Data, byte[] Metadata) Serialize(DomainEvent domainEvent)
        {
            switch (domainEvent)
            {
                case null:
                    throw new ArgumentNullException(nameof(domainEvent));

                case EnvironmentCreated environmentCreated:
                    return EnvironmentCreatedFactory.Serialize(environmentCreated);

                case EnvironmentUpdated environmentUpdated:
                    return EnvironmentUpdatedFactory.Serialize(environmentUpdated);

                default:
                    throw new ArgumentOutOfRangeException(nameof(domainEvent));
            }
        }
    }
}