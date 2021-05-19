using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Exceptions;
using Bechtle.A365.ConfigService.Implementations;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using EventStore.Client;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     base-class for all Event-Store-Streamed objects
    /// </summary>
    public abstract class DomainObject
    {
        private readonly object _eventLock = new object();
        private bool _eventsBeingDrained;
        private IDictionary<Type, Func<ReplayedEvent, bool>> _handlerMapping;

        /// <summary>
        ///     Current Version-Number of this Object
        /// </summary>
        public long CurrentVersion { get; protected set; } = -1;

        /// <summary>
        ///     Version of last Event applied to this Object (disregarding if it was meant for this object)
        /// </summary>
        public long MetaVersion { get; protected set; } = -1;

        /// <summary>
        ///     List of Captured, Successful events applied to this Object
        /// </summary>
        protected List<DomainEvent> CapturedDomainEvents { get; set; } = new List<DomainEvent>();

        /// <summary>
        ///     cache for <see cref="GetEventApplicationMapping" /> using <see cref="_handlerMapping" />
        /// </summary>
        protected IDictionary<Type, Func<ReplayedEvent, bool>> HandlerMapping => _handlerMapping ??= GetEventApplicationMapping();

        /// <summary>
        ///     calculate the size of this object - used to limit the objects kept in the memory-cache at the same time
        /// </summary>
        /// <returns>size of current object in abstract units</returns>
        public abstract long CalculateCacheSize();

        /// <summary>
        ///     apply a snapshot to this object, overriding the current values with the ones from the snapshot.
        ///     actual copy-actions take place here
        /// </summary>
        protected abstract void ApplySnapshotInternal(DomainObject domainObject);

        /// <summary>
        ///     get a mapping of <see cref="DomainEvent" /> to EventHandler. this mapping is used to evaluate all applied events
        /// </summary>
        /// <returns></returns>
        protected abstract IDictionary<Type, Func<ReplayedEvent, bool>> GetEventApplicationMapping();

        /// <summary>
        ///     apply a single <see cref="ReplayedEvent" /> to this object,
        ///     in order to modify its state to a more current one.
        /// </summary>
        /// <param name="replayedEvent"></param>
        public virtual void ApplyEvent(ReplayedEvent replayedEvent)
        {
            // ReSharper disable once UseNullPropagation
            if (replayedEvent is null)
                return;

            if (replayedEvent.DomainEvent is null)
                return;

            if (replayedEvent.Version <= MetaVersion)
                return;

            // if there is a handler for the given DomainEvent, call it
            // if that handler returns true we know the event was meant for this object and
            // we can update CurrentVersion
            if (HandlerMapping.TryGetValue(replayedEvent.DomainEvent.GetType(), out var handler)
                && handler.Invoke(replayedEvent))
                CurrentVersion = replayedEvent.Version;
            MetaVersion = replayedEvent.Version;
        }

        /// <summary>
        ///     apply a snapshot to this object, overriding the current values with the ones from the snapshot.
        /// </summary>
        public virtual void ApplySnapshot(DomainObjectSnapshot snapshot)
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
            }) as DomainObject;

            CurrentVersion = snapshot.Version;
            MetaVersion = snapshot.MetaVersion;

            ApplySnapshotInternal(other);
        }

        /// <summary>
        ///     create the current object as a new Snapshot
        /// </summary>
        /// <returns></returns>
        public virtual DomainObjectSnapshot CreateSnapshot()
            => new DomainObjectSnapshot(GetType().Name,
                                        GetSnapshotIdentifier(),
                                        JsonConvert.SerializeObject(this, GetType(), new JsonSerializerSettings
                                        {
                                            Converters = {new StringEnumConverter(), new IsoDateTimeConverter()},
                                            DateFormatHandling = DateFormatHandling.IsoDateFormat,
                                            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                                            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                                        }),
                                        CurrentVersion,
                                        MetaVersion);

        /// <summary>
        ///     returns the <see cref="CacheItemPriority" /> for this specific object. Defaults to <see cref="CacheItemPriority.Low" />
        /// </summary>
        /// <returns></returns>
        public virtual CacheItemPriority GetCacheItemPriority() => CacheItemPriority.Low;

        /// <summary>
        ///     get a list of all DomainEvent-Types that this DomainObject handles while Streaming
        /// </summary>
        /// <returns></returns>
        public ICollection<string> GetHandledEvents() => HandlerMapping.Keys
                                                                       .Select(t => t.Name)
                                                                       .ToList();

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

                // try writing / splitting / checking until it works or is impossible to work
                while (true)
                {
                    try
                    {
                        // writing - try to write the events
                        MetaVersion = CurrentVersion = await store.WriteEvents(CapturedDomainEvents);
                        CapturedDomainEvents.Clear();
                        IncrementalSnapshotService.QueueSnapshot(CreateSnapshot());

                        return Result.Success();
                    }
                    catch (InvalidMessageSizeException)
                    {
                        // splitting - split messages into smaller pieces if possible
                        int previousNumberOfEvents = CapturedDomainEvents.Count;
                        CapturedDomainEvents = CapturedDomainEvents.SelectMany(e => e.Split())
                                                                   .ToList();

                        // checking - if the split didn't work we can't really continue
                        if (previousNumberOfEvents == CapturedDomainEvents.Count)
                            throw;
                    }
                }
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

                if (!prop.Writable && member is PropertyInfo property)
                    prop.Writable = property.GetSetMethod(true) != null;

                return prop;
            }
        }
    }
}