using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Services;
using Bechtle.A365.ConfigService.Services.Stores;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     base-class for all Event-Store-Streamed objects
    /// </summary>
    public abstract class StreamedObject
    {
        private bool _eventsBeingDrained;

        private readonly object _eventLock = new object();

        /// <summary>
        ///     Current Version-Number of this Object
        /// </summary>
        public long CurrentVersion { get; protected set; } = -1;

        /// <summary>
        ///     List of Captured, Successful events applied to this Object
        /// </summary>
        protected List<DomainEvent> CapturedDomainEvents { get; set; } = new List<DomainEvent>();

        /// <summary>
        ///     apply a series of <see cref="StreamedEvent" /> to this object,
        ///     in order to modify its state to a more current one.
        /// </summary>
        /// <param name="streamedEvents"></param>
        public virtual void ApplyEvents(IEnumerable<StreamedEvent> streamedEvents)
        {
            if (streamedEvents is null)
                return;

            foreach (var streamedEvent in streamedEvents)
                ApplyEvent(streamedEvent);
        }

        /// <summary>
        ///     apply a single <see cref="StreamedEvent" /> to this object,
        ///     in order to modify its state to a more current one.
        /// </summary>
        /// <param name="streamedEvent"></param>
        public virtual void ApplyEvent(StreamedEvent streamedEvent)
        {
            // ReSharper disable once UseNullPropagation
            if (streamedEvent is null)
                return;

            if (streamedEvent.DomainEvent is null)
                return;

            if (streamedEvent.Version <= CurrentVersion)
                return;

            if (ApplyEventInternal(streamedEvent))
                CurrentVersion = streamedEvent.Version;
        }

        /// <summary>
        ///     apply a single <see cref="StreamedEvent" /> to this object,
        ///     in order to modify its state to a more current one.
        /// </summary>
        /// <param name="streamedEvent"></param>
        protected abstract bool ApplyEventInternal(StreamedEvent streamedEvent);

        /// <summary>
        ///     apply a snapshot to this object, overriding the current values with the ones from the snapshot.
        /// </summary>
        public abstract void ApplySnapshot(StreamedObjectSnapshot snapshot);

        /// <summary>
        ///     create the current object as a new Snapshot
        /// </summary>
        /// <returns></returns>
        public virtual StreamedObjectSnapshot CreateSnapshot() => new StreamedObjectSnapshot
        {
            Version = CurrentVersion,
            Data = JsonSerializer.SerializeToUtf8Bytes(this),
            DataType = GetType().Name
        };

        /// <summary>
        ///     Get a list of new events applied to this Object since its creation
        /// </summary>
        /// <returns></returns>
        public virtual DomainEvent[] GetRecordedEvents()
        {
            var retVal = new DomainEvent[CapturedDomainEvents.Count];
            CapturedDomainEvents.CopyTo(retVal);

            return retVal;
        }

        /// <summary>
        ///     write the recorded events to the given <see cref="IEventStore"/>
        /// </summary>
        /// <param name="store"></param>
        /// <returns></returns>
        public virtual async Task<IResult> WriteRecordedEvents(IEventStore store)
        {
            try
            {
                // take lock and see if another instance may already drain this queue
                lock (_eventLock)
                {
                    if (_eventsBeingDrained)
                        return Result.Success();

                    _eventsBeingDrained = true;
                }

                await store.WriteEvents(CapturedDomainEvents);
                CapturedDomainEvents.Clear();

                return Result.Success();
            }
            catch (Exception e)
            {
                return Result.Error($"could not write events to IEventStore: {e.Message}", ErrorCode.Undefined);
            }
            finally
            {
                lock (_eventLock)
                {
                    _eventsBeingDrained = false;
                }
            }
        }

        /// <summary>
        ///     validate all recorded events with the given <see cref="ICommandValidator"/>
        /// </summary>
        /// <param name="validators"></param>
        /// <returns></returns>
        public IDictionary<DomainEvent, IList<IResult>> Validate(IList<ICommandValidator> validators)
            => CapturedDomainEvents.ToDictionary(@event => @event,
                                                 @event => (IList<IResult>) validators.Select(v => v.ValidateDomainEvent(@event))
                                                                                      .ToList())
                                   .Where(kvp => kvp.Value.Any(r => r.IsError))
                                   .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
}