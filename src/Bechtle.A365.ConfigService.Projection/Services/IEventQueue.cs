using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.Projection.Services
{
    /// <summary>
    ///     basic queue to facilitate between <see cref="EventConverter"/> and <see cref="EventProjection"/>
    /// </summary>
    public interface IEventQueue
    {
        /// <summary>
        ///     try to push a new <see cref="DomainEvent"/> into the queue to be processed at a later time
        /// </summary>
        /// <param name="projectedEvent"></param>
        /// <returns>true if <paramref name="projectedEvent"/> was queued successfully</returns>
        bool TryEnqueue(ProjectedEvent projectedEvent);

        /// <summary>
        ///     try to pop an <see cref="DomainEvent"/> from the queue
        /// </summary>
        /// <param name="projectedEvent"></param>
        /// <returns></returns>
        bool TryDequeue(out ProjectedEvent projectedEvent);
    }
}