using System;
using System.Collections.Generic;
using System.Linq;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     Domain-Object representing a Structure for a Configuration
    /// </summary>
    public class ConfigStructure : DomainObject
    {
        /// <inheritdoc />
        public ConfigStructure(StructureIdentifier identifier)
        {
            if (identifier is null)
                throw new ArgumentNullException(nameof(identifier));

            if (string.IsNullOrWhiteSpace(identifier.Name))
                throw new ArgumentNullException(nameof(identifier), $"{nameof(identifier.Name)} is null or empty");

            if (identifier.Version <= 0)
                throw new ArgumentNullException(nameof(identifier), $"{nameof(identifier.Version)} is null or empty");

            Created = false;
            Deleted = false;
            Identifier = new StructureIdentifier(identifier.Name, identifier.Version);
            Keys = new Dictionary<string, string>();
            Variables = new Dictionary<string, string>();
        }

        /// <summary>
        ///     Flag indicating if this Structure has been Created
        /// </summary>
        public bool Created { get; protected set; }

        /// <summary>
        ///     Flag indicating if this Structure has been Deleted
        /// </summary>
        public bool Deleted { get; protected set; }

        /// <inheritdoc cref="StructureIdentifier" />
        public StructureIdentifier Identifier { get; protected set; }

        /// <summary>
        ///     Dictionary containing all hard-coded Values and References to the Environment-Data
        /// </summary>
        public Dictionary<string, string> Keys { get; protected set; }

        /// <summary>
        ///     Modifiable Variables that may be used inside the Structure during Compilation
        /// </summary>
        public Dictionary<string, string> Variables { get; protected set; }

        // use base-size of 12 for Created / Deleted / Identifier + amount of Keys and Variables
        /// <inheritdoc />
        public override long CalculateCacheSize()
            => 12
               + (Keys?.Sum(p => p.Key?.Length ?? 0 + p.Value?.Length ?? 0) ?? 0)
               + (Variables?.Sum(p => p.Key?.Length ?? 0 + p.Value?.Length ?? 0) ?? 0);

        /// <summary>
        ///     Mark this Object as Created, and record all events necessary for this action
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="variables"></param>
        /// <returns></returns>
        public IResult Create(IDictionary<string, string> keys, IDictionary<string, string> variables)
        {
            if (Created)
                return Result.Success();

            Created = true;
            Keys = keys.ToDictionary(pair => pair.Key, pair => pair.Value);
            Variables = variables.ToDictionary(pair => pair.Key, pair => pair.Value);
            CapturedDomainEvents.Add(new StructureCreated(Identifier, keys, variables));

            return Result.Success();
        }

        /// <summary>
        ///     Delete existing Variables, and record all events necessary for this action
        /// </summary>
        /// <param name="keysToRemove"></param>
        /// <returns></returns>
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

        /// <summary>
        ///     Add or Update existing Variables, and record all events necessary for this action
        /// </summary>
        /// <param name="updatedKeys"></param>
        /// <returns></returns>
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

        /// <inheritdoc />
        protected override void ApplySnapshotInternal(DomainObject domainObject)
        {
            if (!(domainObject is ConfigStructure other))
                return;

            Created = other.Created;
            Deleted = other.Deleted;
            Identifier = other.Identifier;
            Keys = other.Keys;
            Variables = other.Variables;
        }

        /// <inheritdoc />
        protected override IDictionary<Type, Func<ReplayedEvent, bool>> GetEventApplicationMapping()
            => new Dictionary<Type, Func<ReplayedEvent, bool>>
            {
                {typeof(StructureCreated), HandleStructureCreatedEvent},
                {typeof(StructureDeleted), HandleStructureDeletedEvent},
                {typeof(StructureVariablesModified), HandleStructureVariablesModifiedEvent}
            };

        /// <inheritdoc />
        protected override string GetSnapshotIdentifier() => Identifier.ToString();

        private bool HandleStructureCreatedEvent(ReplayedEvent replayedEvent)
        {
            if (!(replayedEvent.DomainEvent is StructureCreated created) || created.Identifier != Identifier)
                return false;

            Created = true;
            Keys = created.Keys;
            Variables = created.Variables;
            return true;
        }

        private bool HandleStructureDeletedEvent(ReplayedEvent replayedEvent)
        {
            if (!(replayedEvent.DomainEvent is StructureDeleted deleted) || deleted.Identifier != Identifier)
                return false;

            Deleted = true;
            return true;
        }

        private bool HandleStructureVariablesModifiedEvent(ReplayedEvent replayedEvent)
        {
            if (!(replayedEvent.DomainEvent is StructureVariablesModified modified) || modified.Identifier != Identifier)
                return false;

            foreach (var deletion in modified.ModifiedKeys.Where(action => action.Type == ConfigKeyActionType.Delete))
                if (Keys.ContainsKey(deletion.Key))
                    Keys.Remove(deletion.Key);

            foreach (var change in modified.ModifiedKeys.Where(action => action.Type == ConfigKeyActionType.Set))
                Keys[change.Key] = change.Value;

            return true;
        }
    }
}