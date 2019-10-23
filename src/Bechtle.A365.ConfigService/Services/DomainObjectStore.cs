using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DbObjects;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Services.Stores;

namespace Bechtle.A365.ConfigService.Services
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

    public class StreamedEvent
    {
        public DomainEvent DomainEvent { get; set; }

        public long Version { get; set; }
    }

    public class StreamedEnvironment : StreamedObject
    {
        private readonly DateTime _unixEpoch = new DateTime(1970, 1, 1, 1, 1, 1, DateTimeKind.Utc);

        public bool Created { get; protected set; }

        public bool Deleted { get; protected set; }

        public EnvironmentIdentifier Identifier { get; protected set; }

        public bool IsDefault { get; protected set; }

        public Dictionary<string, ConfigEnvironmentKey> Keys { get; protected set; }

        /// <inheritdoc />
        public StreamedEnvironment(EnvironmentIdentifier identifier)
        {
            if (identifier is null)
                throw new ArgumentNullException(nameof(identifier));

            if (string.IsNullOrWhiteSpace(identifier.Category))
                throw new ArgumentNullException(nameof(identifier.Category));

            if (string.IsNullOrWhiteSpace(identifier.Name))
                throw new ArgumentNullException(nameof(identifier.Name));

            Created = false;
            Deleted = false;
            Identifier = new EnvironmentIdentifier(identifier.Category, identifier.Name);
            IsDefault = false;
            Keys = new Dictionary<string, ConfigEnvironmentKey>();
        }

        /// <inheritdoc />
        protected override bool ApplyEventInternal(StreamedEvent streamedEvent)
        {
            switch (streamedEvent.DomainEvent)
            {
                case DefaultEnvironmentCreated created when created.Identifier == Identifier:
                    HandleDefaultCreated();
                    return true;

                case EnvironmentCreated created when created.Identifier == Identifier:
                    HandleCreated();
                    return true;

                case EnvironmentDeleted deleted when deleted.Identifier == Identifier:
                    HandleDeleted();
                    return true;

                case EnvironmentKeysImported imported when imported.Identifier == Identifier:
                    HandleKeysImported(imported);
                    return true;

                case EnvironmentKeysModified modified when modified.Identifier == Identifier:
                    HandleKeysModified(modified);
                    return true;
            }

            return false;
        }

        private void HandleKeysModified(EnvironmentKeysModified environmentKeysModified)
        {
            foreach (var deletion in environmentKeysModified.ModifiedKeys.Where(action => action.Type == ConfigKeyActionType.Delete))
                if (Keys.ContainsKey(deletion.Key))
                    Keys.Remove(deletion.Key);

            foreach (var change in environmentKeysModified.ModifiedKeys.Where(action => action.Type == ConfigKeyActionType.Set))
                Keys[change.Key] = new ConfigEnvironmentKey
                {
                    Key = change.Key,
                    Value = change.Value,
                    Type = change.ValueType,
                    Description = change.Description,
                    Version = (long) DateTime.UtcNow
                                             .Subtract(_unixEpoch)
                                             .TotalSeconds
                };
        }

        private void HandleKeysImported(EnvironmentKeysImported environmentKeysImported)
        {
            Keys = environmentKeysImported.ModifiedKeys
                                          .Where(action => action.Type == ConfigKeyActionType.Set)
                                          .ToDictionary(
                                              action => action.Key,
                                              action => new ConfigEnvironmentKey
                                              {
                                                  Key = action.Key,
                                                  Value = action.Value,
                                                  Type = action.ValueType,
                                                  Description = action.Description,
                                                  Version = (long) DateTime.UtcNow
                                                                           .Subtract(_unixEpoch)
                                                                           .TotalSeconds
                                              });
        }

        private void HandleDeleted()
        {
            Deleted = true;
        }

        private void HandleCreated()
        {
            Created = true;
        }

        private void HandleDefaultCreated()
        {
            IsDefault = true;
            Created = true;
        }

        /// <inheritdoc />
        public override void ApplySnapshot(StreamedObjectSnapshot snapshot)
        {
            if (snapshot.DataType != GetType().Name)
                return;

            var snapshotData = JsonSerializer.Deserialize<StreamedEnvironment>(snapshot.Data);

            CurrentVersion = snapshot.Version;
            Created = snapshotData.Created;
            Deleted = snapshotData.Deleted;
            Identifier = snapshotData.Identifier;
            IsDefault = snapshotData.IsDefault;
            Keys = snapshotData.Keys;
        }

        /// <summary>
        ///     flag this environment as existing, and create the appropriate events for that
        /// </summary>
        /// <returns></returns>
        public IResult Create(bool isDefault = false)
        {
            if (Created)
                return Result.Success();

            Created = true;
            IsDefault = isDefault;

            if (IsDefault)
                CapturedDomainEvents.Add(new DefaultEnvironmentCreated(Identifier));
            else
                CapturedDomainEvents.Add(new EnvironmentCreated(Identifier));

            return Result.Success();
        }

        /// <summary>
        ///     flag this environment as deleted, and create the appropriate events for that
        /// </summary>
        /// <returns></returns>
        public IResult Delete()
        {
            if (Deleted)
                return Result.Success();

            Deleted = true;
            CapturedDomainEvents.Add(new EnvironmentDeleted(Identifier));

            return Result.Success();
        }

        /// <summary>
        ///     remove some of the held keys, and create the appropriate events for that
        /// </summary>
        /// <param name="keysToRemove"></param>
        /// <returns></returns>
        public IResult DeleteKeys(IReadOnlyList<string> keysToRemove)
        {
            if (keysToRemove is null || !keysToRemove.Any())
                return Result.Error("null or empty list given", ErrorCode.InvalidData);

            if (keysToRemove.Any(k => !Keys.ContainsKey(k)))
                return Result.Error("not all keys could be found in target environment", ErrorCode.NotFound);

            var removedKeys = new Dictionary<string, ConfigEnvironmentKey>(keysToRemove.Count);
            try
            {
                foreach (var deletedKey in keysToRemove)
                    if (Keys.Remove(deletedKey, out var removedKey))
                        removedKeys.Add(deletedKey, removedKey);

                // all items that have actually been removed from this environment are saved as event
                // skipping those that may not be there anymore and reducing the Event-Size or skipping the event entirely
                if (removedKeys.Any())
                    CapturedDomainEvents.Add(new EnvironmentKeysModified(Identifier,
                                                                         removedKeys.Keys
                                                                                    .Select(ConfigKeyAction.Delete)
                                                                                    .ToArray()));

                return Result.Success();
            }
            catch (Exception)
            {
                // restore all keys that have been removed without the operation successfully completing
                foreach (var (key, entry) in removedKeys)
                    Keys[key] = entry;

                return Result.Error("could not remove all keys from the environment", ErrorCode.Undefined);
            }
        }

        /// <summary>
        ///     add new, or change some of the held keys, and create the appropriate events for that
        /// </summary>
        /// <param name="keysToAdd"></param>
        /// <returns></returns>
        public IResult UpdateKeys(IReadOnlyList<ConfigEnvironmentKey> keysToAdd)
        {
            if (keysToAdd is null || !keysToAdd.Any())
                return Result.Error("null or empty list given", ErrorCode.InvalidData);

            var addedKeys = new List<ConfigEnvironmentKey>();
            var updatedKeys = new Dictionary<string, ConfigEnvironmentKey>();

            try
            {
                foreach (var newEntry in keysToAdd)
                {
                    if (Keys.ContainsKey(newEntry.Key))
                    {
                        var oldEntry = Keys[newEntry.Key];
                        if (oldEntry.Description == newEntry.Description
                            && oldEntry.Type == newEntry.Type
                            && oldEntry.Value == newEntry.Value)
                        {
                            // if the key and all metadata is same before and after the change,
                            // we might as well skip this change altogether
                            continue;
                        }

                        updatedKeys.Add(newEntry.Key, Keys[newEntry.Key]);
                        Keys[newEntry.Key] = newEntry;
                    }
                    else
                    {
                        addedKeys.Add(newEntry);
                        Keys.Add(newEntry.Key, newEntry);
                    }
                }

                // all items that have actually been added to or changed in this environment are saved as event
                // skipping those that may not be there anymore and reducing the Event-Size or skipping the event entirely
                var recordedChanges = addedKeys.Concat(updatedKeys.Values).ToList();

                if (recordedChanges.Any())
                    CapturedDomainEvents.Add(
                        new EnvironmentKeysModified(
                            Identifier,
                            recordedChanges.Select(k => ConfigKeyAction.Set(k.Key, k.Value, k.Description, k.Type))
                                           .ToArray()));

                return Result.Success();
            }
            catch (Exception)
            {
                foreach (var addedKey in addedKeys)
                    Keys.Remove(addedKey.Key, out _);
                foreach (var (key, value) in updatedKeys)
                    Keys[key] = value;

                return Result.Error("could not update all keys in the environment", ErrorCode.Undefined);
            }
        }
    }

    public class StreamedObjectStore
    {
        private readonly IEventStore _eventStore;

        /// <inheritdoc />
        public StreamedObjectStore(IEventStore eventStore)
        {
            _eventStore = eventStore;
        }

        public async Task<StreamedEnvironment> Get(EnvironmentIdentifier identifier)
        {
            var environment = new StreamedEnvironment(identifier);

            var latestSnapshot = GetSnapshot(identifier);

            if (!(latestSnapshot is null))
                environment.ApplySnapshot(latestSnapshot);

            // @TODO: start streaming from latestSnapshot.Version
            await _eventStore.ReplayEventsAsStream(tuple =>
            {
                var (recordedEvent, domainEvent) = tuple;

                environment.ApplyEvent(new StreamedEvent
                {
                    Version = recordedEvent.EventNumber,
                    DomainEvent = domainEvent
                });

                return true;
            });

            return environment;
        }

        private StreamedObjectSnapshot GetSnapshot(EnvironmentIdentifier identifier) => null;
    }

    public class StreamedObjectSnapshot
    {
        public string DataType { get; set; }

        public long Version { get; set; }

        public byte[] Data { get; set; }
    }
}