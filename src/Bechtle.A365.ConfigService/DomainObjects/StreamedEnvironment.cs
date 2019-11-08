using System;
using System.Collections.Generic;
using System.Linq;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     Domain-Object representing a Config-Environment which stores a set of Keys from which to build Configurations
    /// </summary>
    public class StreamedEnvironment : StreamedObject
    {
        private readonly DateTime _unixEpoch = new DateTime(1970, 1, 1, 1, 1, 1, DateTimeKind.Utc);

        private List<StreamedEnvironmentKeyPath> _keyPaths;

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
            Keys = new Dictionary<string, StreamedEnvironmentKey>();
        }

        /// <summary>
        ///     Flag indicating if this Environment has been created or not
        /// </summary>
        public bool Created { get; protected set; }

        /// <summary>
        ///     Flag indicating if this Environment has been deleted or not
        /// </summary>
        public bool Deleted { get; protected set; }

        /// <inheritdoc cref="EnvironmentIdentifier" />
        public EnvironmentIdentifier Identifier { get; protected set; }

        /// <summary>
        ///     Flag indicating if this is the Default-Environment of its Category
        /// </summary>
        public bool IsDefault { get; protected set; }

        /// <summary>
        ///     Trees of Paths that represent all Keys in the Environment
        /// </summary>
        public List<StreamedEnvironmentKeyPath> KeyPaths => _keyPaths ??= GenerateKeyPaths();

        /// <summary>
        ///     Actual Data of this Environment
        /// </summary>
        public Dictionary<string, StreamedEnvironmentKey> Keys { get; protected set; }

        // 10 for identifier, 5 for rest, each Key, each Path (recursively)
        /// <inheritdoc />
        public override long CalculateCacheSize()
            => Identifier.Category.Length
               + Identifier.Name.Length
               + (Keys?.Sum(p => p.Key.Length
                                 + p.Value.Description?.Length ?? 0
                                 + p.Value.Key?.Length ?? 0
                                 + p.Value.Type?.Length ?? 0
                                 + p.Value.Value?.Length ?? 0
                                 + 8 //p.Value.Version
                  ) ?? 0)
               + CountGeneratedPaths();

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
        public IResult DeleteKeys(ICollection<string> keysToRemove)
        {
            if (keysToRemove is null || !keysToRemove.Any())
                return Result.Error("null or empty list given", ErrorCode.InvalidData);

            if (keysToRemove.Any(k => !Keys.ContainsKey(k)))
                return Result.Error("not all keys could be found in target environment", ErrorCode.NotFound);

            var removedKeys = new Dictionary<string, StreamedEnvironmentKey>(keysToRemove.Count);
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
        ///     get the values of <see cref="Keys" /> as a simple <see cref="Dictionary{String,String}" />
        /// </summary>
        /// <returns></returns>
        public IDictionary<string, string> GetKeysAsDictionary() => Keys.Values.ToDictionary(e => e.Key, e => e.Value);

        /// <summary>
        ///     overwrite all existing keys with the ones in <paramref name="keysToImport" />
        /// </summary>
        /// <param name="keysToImport"></param>
        /// <returns></returns>
        public IResult ImportKeys(ICollection<StreamedEnvironmentKey> keysToImport)
        {
            // copy dict as backup
            var oldKeys = Keys.ToDictionary(_ => _.Key, _ => _.Value);

            try
            {
                var newKeys = keysToImport.ToDictionary(k => k.Key, k => k);
                Keys = newKeys;

                CapturedDomainEvents.Add(new EnvironmentKeysImported(
                                             Identifier,
                                             newKeys.Values
                                                    .OrderBy(k => k.Key)
                                                    .Select(k => ConfigKeyAction.Set(k.Key, k.Value, k.Description, k.Type))
                                                    .ToArray()));

                _keyPaths = null;

                return Result.Success();
            }
            catch (Exception)
            {
                // restore backup
                Keys = oldKeys;

                return Result.Error("could not import all keys into this environment", ErrorCode.Undefined);
            }
        }

        /// <summary>
        ///     add new, or change some of the held keys, and create the appropriate events for that
        /// </summary>
        /// <param name="keysToAdd"></param>
        /// <returns></returns>
        public IResult UpdateKeys(ICollection<StreamedEnvironmentKey> keysToAdd)
        {
            if (keysToAdd is null || !keysToAdd.Any())
                return Result.Error("null or empty list given", ErrorCode.InvalidData);

            var addedKeys = new List<StreamedEnvironmentKey>();
            var updatedKeys = new Dictionary<string, StreamedEnvironmentKey>();

            try
            {
                foreach (var newEntry in keysToAdd)
                    if (Keys.ContainsKey(newEntry.Key))
                    {
                        var oldEntry = Keys[newEntry.Key];
                        if (oldEntry.Description == newEntry.Description
                            && oldEntry.Type == newEntry.Type
                            && oldEntry.Value == newEntry.Value)
                            // if the key and all metadata is same before and after the change,
                            // we might as well skip this change altogether
                            continue;

                        updatedKeys.Add(newEntry.Key, Keys[newEntry.Key]);
                        Keys[newEntry.Key] = newEntry;
                    }
                    else
                    {
                        addedKeys.Add(newEntry);
                        Keys.Add(newEntry.Key, newEntry);
                    }

                // all items that have actually been added to or changed in this environment are saved as event
                // skipping those that may not be there anymore and reducing the Event-Size or skipping the event entirely
                // ---
                // updatedKeys maps key => oldValue, so the old value can be restored if something goes wrong
                // this means we have to get the current/new/overwritten state based on the updatedKeys.Keys
                var recordedChanges = addedKeys.Concat(updatedKeys.Keys.Select(k => Keys[k]))
                                               .ToList();

                if (recordedChanges.Any())
                    CapturedDomainEvents.Add(
                        new EnvironmentKeysModified(
                            Identifier,
                            recordedChanges.Select(k => ConfigKeyAction.Set(k.Key, k.Value, k.Description, k.Type))
                                           .ToArray()));

                _keyPaths = null;

                return Result.Success();
            }
            catch (Exception)
            {
                foreach (var addedKey in addedKeys)
                    Keys.Remove(addedKey.Key);
                foreach (var (key, value) in updatedKeys)
                    Keys[key] = value;

                return Result.Error("could not update all keys in the environment", ErrorCode.Undefined);
            }
        }

        /// <inheritdoc />
        protected override void ApplySnapshotInternal(StreamedObject streamedObject)
        {
            if (!(streamedObject is StreamedEnvironment other))
                return;

            Created = other.Created;
            Deleted = other.Deleted;
            Identifier = other.Identifier;
            IsDefault = other.IsDefault;
            Keys = other.Keys;
        }

        /// <inheritdoc />
        protected override IDictionary<Type, Func<StreamedEvent, bool>> GetEventApplicationMapping()
            => new Dictionary<Type, Func<StreamedEvent, bool>>
            {
                {typeof(DefaultEnvironmentCreated), HandleDefaultEnvironmentCreatedEvent},
                {typeof(EnvironmentCreated), HandleEnvironmentCreatedEvent},
                {typeof(EnvironmentDeleted), HandleEnvironmentDeletedEvent},
                {typeof(EnvironmentKeysImported), HandleEnvironmentKeysImportedEvent},
                {typeof(EnvironmentKeysModified), HandleEnvironmentKeysModifiedEvent}
            };

        /// <inheritdoc />
        protected override string GetSnapshotIdentifier() => Identifier.ToString();

        private long CountGeneratedPaths()
        {
            var computedCost = 0;
            var stack = _keyPaths is null
                            ? new Stack<StreamedEnvironmentKeyPath>()
                            : new Stack<StreamedEnvironmentKeyPath>(_keyPaths);

            while (stack.TryPop(out var item))
            {
                computedCost += item.Path.Length;
                computedCost += item.FullPath.Length;

                foreach (var child in item.Children)
                    stack.Push(child);
            }

            return computedCost;
        }

        private List<StreamedEnvironmentKeyPath> GenerateKeyPaths()
        {
            var roots = new List<StreamedEnvironmentKeyPath>();

            foreach (var (key, _) in Keys.OrderBy(k => k.Key))
            {
                var parts = key.Split('/');

                var rootPart = parts.First();
                var root = roots.FirstOrDefault(p => p.Path.Equals(rootPart, StringComparison.InvariantCultureIgnoreCase));

                if (root is null)
                {
                    root = new StreamedEnvironmentKeyPath
                    {
                        Path = rootPart,
                        Parent = null,
                        FullPath = rootPart,
                        Children = new List<StreamedEnvironmentKeyPath>()
                    };

                    roots.Add(root);
                }

                var current = root;

                foreach (var part in parts.Skip(1))
                {
                    var next = current.Children.FirstOrDefault(p => p.Path.Equals(part, StringComparison.InvariantCultureIgnoreCase));

                    if (next is null)
                    {
                        next = new StreamedEnvironmentKeyPath
                        {
                            Path = part,
                            Parent = current,
                            Children = new List<StreamedEnvironmentKeyPath>(),
                            FullPath = current.FullPath + '/' + part
                        };
                        current.Children.Add(next);
                    }

                    current = next;
                }
            }

            return roots;
        }

        private bool HandleDefaultEnvironmentCreatedEvent(StreamedEvent streamedEvent)
        {
            if (!(streamedEvent.DomainEvent is EnvironmentCreated created) || created.Identifier != Identifier)
                return false;

            IsDefault = true;
            Created = true;
            return true;
        }

        private bool HandleEnvironmentCreatedEvent(StreamedEvent streamedEvent)
        {
            if (!(streamedEvent.DomainEvent is DefaultEnvironmentCreated created) || created.Identifier != Identifier)
                return false;

            Created = true;
            return true;
        }

        private bool HandleEnvironmentDeletedEvent(StreamedEvent streamedEvent)
        {
            if (!(streamedEvent.DomainEvent is EnvironmentDeleted deleted) || deleted.Identifier != Identifier)
                return false;

            Deleted = true;
            return true;
        }

        private bool HandleEnvironmentKeysImportedEvent(StreamedEvent streamedEvent)
        {
            if (!(streamedEvent.DomainEvent is EnvironmentKeysImported imported) || imported.Identifier != Identifier)
                return false;

            Keys = imported.ModifiedKeys
                           .Where(action => action.Type == ConfigKeyActionType.Set)
                           .ToDictionary(
                               action => action.Key,
                               action => new StreamedEnvironmentKey
                               {
                                   Key = action.Key,
                                   Value = action.Value,
                                   Type = action.ValueType,
                                   Description = action.Description,
                                   Version = (long) DateTime.UtcNow
                                                            .Subtract(_unixEpoch)
                                                            .TotalSeconds
                               });

            _keyPaths = null;
            return true;
        }

        private bool HandleEnvironmentKeysModifiedEvent(StreamedEvent streamedEvent)
        {
            if (!(streamedEvent.DomainEvent is EnvironmentKeysModified modified) || modified.Identifier != Identifier)
                return false;

            foreach (var deletion in modified.ModifiedKeys.Where(action => action.Type == ConfigKeyActionType.Delete))
                if (Keys.ContainsKey(deletion.Key))
                    Keys.Remove(deletion.Key);

            foreach (var change in modified.ModifiedKeys.Where(action => action.Type == ConfigKeyActionType.Set))
                Keys[change.Key] = new StreamedEnvironmentKey
                {
                    Key = change.Key,
                    Value = change.Value,
                    Type = change.ValueType,
                    Description = change.Description,
                    Version = (long) DateTime.UtcNow
                                             .Subtract(_unixEpoch)
                                             .TotalSeconds
                };

            _keyPaths = null;
            return true;
        }
    }
}