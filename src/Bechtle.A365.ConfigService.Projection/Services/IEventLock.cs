using System;

namespace Bechtle.A365.ConfigService.Projection.Services
{
    /// <summary>
    ///     component to lock events from being processed from other nodes of the same type
    /// </summary>
    public interface IEventLock
    {
        /// <summary>
        ///     attempt to lock a given <paramref name="eventId"/> for the given <paramref name="nodeId"/>
        /// </summary>
        /// <param name="eventId">generic event-id, needs to be consistent between calls</param>
        /// <param name="nodeId">generic node-id / worker-id, needs to be consistent within a working group (e.g. environment + database)</param>
        /// <returns>true if the event could be locked for this instance, false if another node is already working on the event</returns>
        Guid TryLockEvent(string eventId, string nodeId, TimeSpan duration);

        /// <summary>
        ///     try to remove an existing lock by passing the ID returned from <see cref="TryLockEvent"/>
        /// </summary>
        /// <param name="eventId">generic event-id, needs to be consistent between calls</param>
        /// <param name="nodeId">generic node-id / worker-id, needs to be consistent within a working group (e.g. environment + database)</param>
        /// <param name="owningLockId">Id returned from successful calls to <see cref="TryLockEvent"/></param>
        bool TryUnlockEvent(string eventId, string nodeId, Guid owningLockId);
    }
}