using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Interfaces.Stores;

namespace Bechtle.A365.ConfigService.Interfaces
{
    /// <summary>
    ///     component that can deserialize Metadata and the actual Data from a given <see cref="StoredEvent"/>
    /// </summary>
    public interface IEventDeserializer
    {
        /// <summary>
        ///     inspect the given <see cref="StoredEvent"/> and deserialize its <see cref="StoredEvent.Data"/> property
        /// </summary>
        /// <param name="storedEvent"></param>
        /// <param name="domainEvent"></param>
        /// <returns></returns>
        bool ToDomainEvent(StoredEvent storedEvent, out DomainEvent domainEvent);

        /// <summary>
        ///     inspect the given <see cref="StoredEvent"/> and deserialize its <see cref="StoredEvent.Metadata"/> property
        /// </summary>
        /// <param name="storedEvent"></param>
        /// <param name="metadata"></param>
        /// <returns></returns>
        bool ToMetadata(StoredEvent storedEvent, out DomainEventMetadata metadata);
    }
}