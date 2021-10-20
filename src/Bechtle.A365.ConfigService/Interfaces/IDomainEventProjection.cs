using System.Threading.Tasks;
using Bechtle.A365.ServiceBase.EventStore.Abstractions;

namespace Bechtle.A365.ConfigService.Interfaces
{
    /// <summary>
    ///     Component that can project the changes recorded in
    ///     <typeparamref name="TDomainEvent" /> to an underlying store
    /// </summary>
    /// <typeparam name="TDomainEvent">type of DomainEvent this component can project</typeparam>
    public interface IDomainEventProjection<TDomainEvent>
    {
        /// <summary>
        ///     Project the changes given in <paramref name="domainEvent" />
        /// </summary>
        /// <param name="eventHeader">metadata for the given domain-event</param>
        /// <param name="domainEvent">domain-event containing the changes to project</param>
        /// <returns>task that contains the projection</returns>
        public Task ProjectChanges(
            StreamedEventHeader eventHeader,
            IDomainEvent<TDomainEvent> domainEvent);
    }
}
