using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Serialization;
using Bechtle.A365.ConfigService.Services;
using Bechtle.A365.ConfigService.Services.Stores;
using Microsoft.Extensions.Caching.Memory;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     base-class for all Event-Store-Streamed objects
    /// </summary>
    public abstract class StreamedObject
    {
        private readonly object _eventLock = new object();
        private bool _eventsBeingDrained;

        /// <summary>
        ///     Current Version-Number of this Object
        /// </summary>
        public long CurrentVersion { get; protected set; } = -1;

        /// <summary>
        ///     List of Captured, Successful events applied to this Object
        /// </summary>
        protected List<DomainEvent> CapturedDomainEvents { get; set; } = new List<DomainEvent>();

        /// <summary>
        ///     calculate the size of this object - used to limit the objects kept in the memory-cache at the same time
        /// </summary>
        /// <returns>size of current object in abstract units</returns>
        public abstract long CalculateCacheSize();

        /// <summary>
        ///     apply a single <see cref="StreamedEvent" /> to this object,
        ///     in order to modify its state to a more current one.
        /// </summary>
        /// <param name="streamedEvent"></param>
        protected abstract bool ApplyEventInternal(StreamedEvent streamedEvent);

        /// <summary>
        ///     apply a snapshot to this object, overriding the current values with the ones from the snapshot.
        ///     actual copy-actions take place here
        /// </summary>
        protected abstract void ApplySnapshotInternal(StreamedObject streamedObject);

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
        ///     apply a snapshot to this object, overriding the current values with the ones from the snapshot.
        /// </summary>
        public virtual void ApplySnapshot(StreamedObjectSnapshot snapshot)
        {
            var actualType = GetType();

            if (snapshot.DataType != actualType.Name)
                return;

            var other = JsonSerializer.Deserialize(snapshot.JsonData, actualType) as StreamedObject;

            CurrentVersion = snapshot.Version;

            ApplySnapshotInternal(other);
        }

        /// <summary>
        ///     create the current object as a new Snapshot
        /// </summary>
        /// <returns></returns>
        public virtual StreamedObjectSnapshot CreateSnapshot() => new StreamedObjectSnapshot
        {
            Identifier = GetSnapshotIdentifier(),
            Version = CurrentVersion,
            DataType = GetType().Name,
            // using GetType to point the serializer to the ACTUAL class it needs to inspect
            JsonData = JsonSerializer.Serialize(this, GetType(), new JsonSerializerOptions
            {
                Converters =
                {
                    new JsonIsoDateConverter(),
                    new JsonStringEnumConverter()
                }
            })
        };

        /// <summary>
        ///     returns the <see cref="CacheItemPriority" /> for this specific object. Defaults to <see cref="CacheItemPriority.Low" />
        /// </summary>
        /// <returns></returns>
        public virtual CacheItemPriority GetCacheItemPriority() => CacheItemPriority.Low;

        /// <summary>
        ///     validate all recorded events with the given <see cref="ICommandValidator" />
        /// </summary>
        /// <param name="validators"></param>
        /// <returns></returns>
        public IDictionary<DomainEvent, IList<IResult>> Validate(IList<ICommandValidator> validators)
            => CapturedDomainEvents.ToDictionary(@event => @event,
                                                 @event => (IList<IResult>) validators.Select(v => v.ValidateDomainEvent(@event))
                                                                                      .ToList())
                                   .Where(kvp => kvp.Value.Any(r => r.IsError))
                                   .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        /// <summary>
        ///     write the recorded events to the given <see cref="IEventStore" />
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

                CurrentVersion = await store.WriteEvents(CapturedDomainEvents);
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
        ///     retrieve a generic identifier to tie a snapshot to this object
        /// </summary>
        /// <returns></returns>
        protected virtual string GetSnapshotIdentifier() => GetType().Name;
    }
}