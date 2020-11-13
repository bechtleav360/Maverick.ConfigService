using System;
using System.Collections.Generic;
using System.Linq;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Microsoft.Extensions.Caching.Memory;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     Named Layer of Keys, to be assigned to one or more Environments
    /// </summary>
    public class EnvironmentLayer : DomainObject
    {
        private List<EnvironmentLayerKeyPath> _keyPaths;

        /// <inheritdoc />
        public EnvironmentLayer(LayerIdentifier identifier)
        {
            if (identifier is null)
                throw new ArgumentNullException(nameof(identifier));

            if (string.IsNullOrWhiteSpace(identifier.Name))
                throw new ArgumentNullException(nameof(identifier), $"{nameof(identifier.Name)} is null or empty");

            Created = false;
            Deleted = false;
            Identifier = identifier;
            Keys = new Dictionary<string, EnvironmentLayerKey>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        ///     Flag indicating if this Environment has been created or not
        /// </summary>
        public bool Created { get; protected set; }

        /// <summary>
        ///     Flag indicating if this Environment has been deleted or not
        /// </summary>
        public bool Deleted { get; protected set; }

        /// <inheritdoc cref="LayerIdentifier" />
        public LayerIdentifier Identifier { get; set; }

        /// <summary>
        ///     Trees of Paths that represent all Keys in this Layer
        /// </summary>
        public List<EnvironmentLayerKeyPath> KeyPaths => _keyPaths ??= GenerateKeyPaths();

        /// <summary>
        ///     Actual Data of this Environment
        /// </summary>
        public Dictionary<string, EnvironmentLayerKey> Keys { get; protected set; }

        /// <inheritdoc />
        public override long CalculateCacheSize()
            => Identifier.Name.Length
               + (Keys?.Sum(p => p.Key.Length
                                 + p.Value.Description?.Length ?? 0
                                 + p.Value.Key?.Length ?? 0
                                 + p.Value.Type?.Length ?? 0
                                 + p.Value.Value?.Length ?? 0
                                 + 8 //p.Value.Version
                  ) ?? 0)
               + CountGeneratedPaths();

        /// <summary>
        ///     flag this layer as existing, and create the appropriate events for that
        /// </summary>
        /// <returns></returns>
        public IResult Create()
        {
            if (Created)
                return Result.Success();

            Created = true;
            Deleted = false;
            CapturedDomainEvents.Add(new EnvironmentLayerCreated(Identifier));

            return Result.Success();
        }

        /// <summary>
        ///     flag this layer as deleted, and create the appropriate events for that
        /// </summary>
        /// <returns></returns>
        public IResult Delete()
        {
            if (Deleted)
                return Result.Success();

            Created = false;
            Deleted = true;
            CapturedDomainEvents.Add(new EnvironmentLayerDeleted(Identifier));

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

            var removedKeys = new Dictionary<string, EnvironmentLayerKey>(keysToRemove.Count);
            try
            {
                foreach (var deletedKey in keysToRemove)
                    if (Keys.Remove(deletedKey, out var removedKey))
                        removedKeys.Add(deletedKey, removedKey);

                // all items that have actually been removed from this environment are saved as event
                // skipping those that may not be there anymore and reducing the Event-Size or skipping the event entirely
                if (removedKeys.Any())
                    CapturedDomainEvents.Add(new EnvironmentLayerKeysModified(Identifier,
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

                return Result.Error("could not remove all keys from the environment-layer", ErrorCode.Undefined);
            }
        }

        /// <inheritdoc />
        public override CacheItemPriority GetCacheItemPriority() => CacheItemPriority.High;

        /// <summary>
        ///     get the values of <see cref="Keys" /> as a simple <see cref="Dictionary{String,String}" />
        /// </summary>
        /// <returns></returns>
        public IDictionary<string, string> GetKeysAsDictionary() => Keys.Values.ToDictionary(e => e.Key, e => e.Value, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     overwrite all existing keys with the ones in <paramref name="keysToImport" />
        /// </summary>
        /// <param name="keysToImport"></param>
        /// <returns></returns>
        public IResult ImportKeys(ICollection<EnvironmentLayerKey> keysToImport)
        {
            // copy dict as backup
            var oldKeys = Keys.ToDictionary(_ => _.Key, _ => _.Value);

            try
            {
                var newKeys = keysToImport.ToDictionary(k => k.Key, k => k, StringComparer.OrdinalIgnoreCase);

                Created = true;
                Keys = newKeys;

                CapturedDomainEvents.Add(new EnvironmentLayerKeysImported(
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

                return Result.Error("could not import all keys into this environment-layer", ErrorCode.Undefined);
            }
        }

        /// <summary>
        ///     add new, or change some of the held keys, and create the appropriate events for that
        /// </summary>
        /// <param name="keysToAdd"></param>
        /// <returns></returns>
        public IResult UpdateKeys(ICollection<EnvironmentLayerKey> keysToAdd)
        {
            if (keysToAdd is null || !keysToAdd.Any())
                return Result.Error("null or empty list given", ErrorCode.InvalidData);

            var addedKeys = new List<EnvironmentLayerKey>();
            var updatedKeys = new Dictionary<string, EnvironmentLayerKey>();

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
                        new EnvironmentLayerKeysModified(
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

                return Result.Error("could not update all keys in the environment-layer", ErrorCode.Undefined);
            }
        }

        /// <inheritdoc />
        protected override void ApplySnapshotInternal(DomainObject domainObject)
        {
            if (!(domainObject is EnvironmentLayer other))
                return;

            Created = other.Created;
            Deleted = other.Deleted;
            Identifier = other.Identifier;
            Keys = other.Keys;
        }

        /// <inheritdoc />
        protected override IDictionary<Type, Func<ReplayedEvent, bool>> GetEventApplicationMapping()
            => new Dictionary<Type, Func<ReplayedEvent, bool>>
            {
                {typeof(EnvironmentLayerCreated), HandleEnvironmentLayerCreated},
                {typeof(EnvironmentLayerDeleted), HandleEnvironmentLayerDeleted},
                {typeof(EnvironmentLayerKeysModified), HandleEnvironmentLayerKeysModified},
                {typeof(EnvironmentLayerKeysImported), HandleEnvironmentLayerKeysImported}
            };

        /// <inheritdoc />
        protected override string GetSnapshotIdentifier() => Identifier.ToString();

        private long CountGeneratedPaths()
        {
            var computedCost = 0;
            var stack = _keyPaths is null
                            ? new Stack<EnvironmentLayerKeyPath>()
                            : new Stack<EnvironmentLayerKeyPath>(_keyPaths);

            while (stack.TryPop(out var item))
            {
                computedCost += item.Path.Length;
                computedCost += item.FullPath.Length;

                foreach (var child in item.Children)
                    stack.Push(child);
            }

            return computedCost;
        }

        private List<EnvironmentLayerKeyPath> GenerateKeyPaths()
        {
            var roots = new List<EnvironmentLayerKeyPath>();

            foreach (var (key, _) in Keys.OrderBy(k => k.Key))
            {
                var parts = key.Split('/');

                var rootPart = parts.First();
                var root = roots.FirstOrDefault(p => p.Path.Equals(rootPart, StringComparison.InvariantCultureIgnoreCase));

                if (root is null)
                {
                    root = new EnvironmentLayerKeyPath(rootPart);
                    roots.Add(root);
                }

                var current = root;

                foreach (var part in parts.Skip(1))
                {
                    var next = current.Children.FirstOrDefault(p => p.Path.Equals(part, StringComparison.InvariantCultureIgnoreCase));

                    if (next is null)
                    {
                        next = new EnvironmentLayerKeyPath(part, current);
                        current.Children.Add(next);
                    }

                    current = next;
                }
            }

            return roots;
        }

        private bool HandleEnvironmentLayerCreated(ReplayedEvent replayedEvent)
        {
            if (!(replayedEvent.DomainEvent is EnvironmentLayerCreated created) || created.Identifier != Identifier)
                return false;

            Created = true;
            Deleted = false;
            return true;
        }

        private bool HandleEnvironmentLayerDeleted(ReplayedEvent replayedEvent)
        {
            if (!(replayedEvent.DomainEvent is EnvironmentLayerDeleted deleted) || deleted.Identifier != Identifier)
                return false;

            Created = false;
            Deleted = true;
            return true;
        }

        private bool HandleEnvironmentLayerKeysImported(ReplayedEvent replayedEvent)
        {
            if (!(replayedEvent.DomainEvent is EnvironmentLayerKeysImported imported) || imported.Identifier != Identifier)
                return false;

            Created = true;
            Keys = imported.ModifiedKeys
                           .Where(action => action.Type == ConfigKeyActionType.Set)
                           .ToDictionary(
                               action => action.Key,
                               action => new EnvironmentLayerKey(action.Key,
                                                                 action.Value,
                                                                 action.ValueType,
                                                                 action.Description,
                                                                 (long) DateTime.UtcNow
                                                                                .Subtract(DateTime.UnixEpoch)
                                                                                .TotalSeconds));

            _keyPaths = null;
            return true;
        }

        private bool HandleEnvironmentLayerKeysModified(ReplayedEvent replayedEvent)
        {
            if (!(replayedEvent.DomainEvent is EnvironmentLayerKeysModified modified) || modified.Identifier != Identifier)
                return false;

            foreach (var deletion in modified.ModifiedKeys.Where(action => action.Type == ConfigKeyActionType.Delete))
                if (Keys.ContainsKey(deletion.Key))
                    Keys.Remove(deletion.Key);

            foreach (var change in modified.ModifiedKeys.Where(action => action.Type == ConfigKeyActionType.Set))
            {
                if (Keys.ContainsKey(change.Key))
                    Keys.Remove(change.Key);

                Keys[change.Key] = new EnvironmentLayerKey(change.Key,
                                                           change.Value,
                                                           change.ValueType,
                                                           change.Description,
                                                           (long) DateTime.UtcNow
                                                                          .Subtract(DateTime.UnixEpoch)
                                                                          .TotalSeconds);
            }

            _keyPaths = null;
            return true;
        }
    }
}