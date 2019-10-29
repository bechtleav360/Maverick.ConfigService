using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    public class StreamedStructure : StreamedObject
    {
        public bool Created { get; protected set; }

        public bool Deleted { get; protected set; }

        public StructureIdentifier Identifier { get; protected set; }

        public Dictionary<string, string> Keys { get; protected set; }

        public Dictionary<string, string> Variables { get; protected set; }

        /// <inheritdoc />
        public StreamedStructure(StructureIdentifier identifier)
        {
            if (identifier is null)
                throw new ArgumentNullException(nameof(identifier));

            if (string.IsNullOrWhiteSpace(identifier.Name))
                throw new ArgumentNullException(nameof(identifier.Name));

            if (identifier.Version <= 0)
                throw new ArgumentNullException(nameof(identifier.Version));

            Created = false;
            Deleted = false;
            Identifier = new StructureIdentifier(identifier.Name, identifier.Version);
            Keys = new Dictionary<string, string>();
        }

        /// <inheritdoc />
        protected override bool ApplyEventInternal(StreamedEvent streamedEvent)
        {
            switch (streamedEvent.DomainEvent)
            {
                case StructureCreated created when created.Identifier == Identifier:
                    Created = true;
                    Keys = created.Keys;
                    Variables = created.Variables;
                    return true;

                case StructureDeleted deleted when deleted.Identifier == Identifier:
                    Deleted = true;
                    return true;

                case StructureVariablesModified modified when modified.Identifier == Identifier:
                    foreach (var deletion in modified.ModifiedKeys.Where(action => action.Type == ConfigKeyActionType.Delete))
                        if (Keys.ContainsKey(deletion.Key))
                            Keys.Remove(deletion.Key);

                    foreach (var change in modified.ModifiedKeys.Where(action => action.Type == ConfigKeyActionType.Set))
                        Keys[change.Key] = change.Value;
                    return true;
            }

            return false;
        }

        /// <inheritdoc />
        public override void ApplySnapshot(StreamedObjectSnapshot snapshot)
        {
            if (snapshot.DataType != GetType().Name)
                return;

            var other = JsonSerializer.Deserialize<StreamedStructure>(snapshot.Data);

            CurrentVersion = snapshot.Version;
            Created = other.Created;
            Deleted = other.Deleted;
            Identifier = other.Identifier;
            Keys = other.Keys;
        }

        // use base-size of 12 for Created / Deleted / Identifier + amount of Keys and Variables
        /// <inheritdoc />
        protected override long CalculateCacheSize()
            => 12
               + Keys.Count
               + Variables.Count;

        public IResult Create(IDictionary<string, string> keys, IDictionary<string, string> variables)
        {
            if (Created)
                return Result.Success();

            Created = true;
            CapturedDomainEvents.Add(new StructureCreated(Identifier, keys, variables));

            return Result.Success();
        }

        public IResult ModifyVariables(IDictionary<string, string> updatedKeys)
        {
            if (updatedKeys is null || !updatedKeys.Any())
                return Result.Error("null or empty variables given", ErrorCode.InvalidData);

            // record all new keys to remove in case of exception
            var recordedAdditions = new List<string>();

            // maps key to old value - to revert back in case of exception
            var recordedUpdates = new Dictionary<string, string>();

            try
            {
                foreach (var (key, value) in updatedKeys)
                {
                    if (Variables.ContainsKey(key))
                    {
                        recordedUpdates[key] = Variables[key];
                        Variables[key] = value;
                    }
                    else
                    {
                        recordedAdditions.Add(key);
                        Variables[key] = value;
                    }
                }

                // all items that have actually been added to or changed in this structure are saved as event
                // skipping those that may not be there anymore and reducing the Event-Size or skipping the event entirely
                var recordedChanges = recordedAdditions.Concat(updatedKeys.Keys).ToList();

                if (recordedChanges.Any())
                    CapturedDomainEvents.Add(
                        new StructureVariablesModified(
                            Identifier,
                            recordedChanges.Select(k => ConfigKeyAction.Set(k, Variables[k]))
                                           .ToArray()));

                return Result.Success();
            }
            catch (Exception)
            {
                foreach (var addedKey in recordedAdditions)
                    Variables.Remove(addedKey);
                foreach (var (key, value) in recordedUpdates)
                    Variables[key] = value;

                return Result.Error("could not update all variables for this Structure", ErrorCode.Undefined);
            }
        }

        public IResult DeleteVariables(ICollection<string> keysToRemove)
        {
            if (keysToRemove is null || !keysToRemove.Any())
                return Result.Error("null or empty list given", ErrorCode.InvalidData);

            if (keysToRemove.Any(k => !Variables.ContainsKey(k)))
                return Result.Error("not all variables could be found in target structure", ErrorCode.NotFound);

            var removedKeys = new Dictionary<string, string>(keysToRemove.Count);
            try
            {
                foreach (var deletedKey in keysToRemove)
                    if (Variables.Remove(deletedKey, out var removedKey))
                        removedKeys.Add(deletedKey, removedKey);

                // all items that have actually been removed from this structure are saved as event
                // skipping those that may not be there anymore and reducing the Event-Size or skipping the event entirely
                if (removedKeys.Any())
                    CapturedDomainEvents.Add(new StructureVariablesModified(
                                                 Identifier,
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

                return Result.Error("could not remove all variables from the structure", ErrorCode.Undefined);
            }
        }
    }
}