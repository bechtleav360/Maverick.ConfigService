using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Services;
using Bechtle.A365.ConfigService.Services.Stores;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using JsonProperty = Newtonsoft.Json.Serialization.JsonProperty;

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

            var other = JsonConvert.DeserializeObject(snapshot.JsonData, actualType, new JsonSerializerSettings
            {
                Converters = {new StringEnumConverter(), new IsoDateTimeConverter()},
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                ContractResolver = new CustomContractResolver()
            }) as StreamedObject;

            CurrentVersion = snapshot.Version;

            ApplySnapshotInternal(other);
        }

        /// <summary>
        ///     create the current object as a new Snapshot
        /// </summary>
        /// <returns></returns>
        public virtual StreamedObjectSnapshot CreateSnapshot()
        {
            var snapshot = new StreamedObjectSnapshot
            {
                Identifier = GetSnapshotIdentifier(),
                Version = CurrentVersion,
                DataType = GetType().Name,
                JsonData = JsonConvert.SerializeObject(this, GetType(), new JsonSerializerSettings
                {
                    Converters = {new StringEnumConverter(), new IsoDateTimeConverter()},
                    DateFormatHandling = DateFormatHandling.IsoDateFormat,
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                })
            };
            return snapshot;
        }

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
                // if we don't have anything to store, we return immediately
                if (!CapturedDomainEvents.Any())
                    return Result.Success();

                // take lock and see if another instance may already drain this queue
                lock (_eventLock)
                {
                    if (_eventsBeingDrained)
                        return Result.Success();

                    _eventsBeingDrained = true;
                }

                CurrentVersion = await store.WriteEvents(CapturedDomainEvents);
                CapturedDomainEvents.Clear();
                IncrementalSnapshotService.QueueSnapshot(CreateSnapshot());

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

        private class CustomContractResolver : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var prop = base.CreateProperty(member, memberSerialization);

                if (!prop.Writable)
                    if (member is PropertyInfo property)
                        prop.Writable = property.GetSetMethod(true) != null;

                return prop;
            }
        }
    }
}