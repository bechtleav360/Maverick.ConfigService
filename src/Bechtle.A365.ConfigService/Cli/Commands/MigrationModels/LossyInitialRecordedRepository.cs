using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using EventStore.Client;
using Newtonsoft.Json;

namespace Bechtle.A365.ConfigService.Cli.Commands.MigrationModels
{
    /// <summary>
    ///     Records and represents the total state of a ConfigService-EventStream.
    ///     Uses this information to generate the equivalent state in the migrated form.
    /// </summary>
    public class LossyInitialRecordedRepository : IState
    {
        // public so it can be properly de-/serialized
        /// <summary>
        ///     List of environments that need to be remade
        /// </summary>
        public readonly List<InitialEnvRepr> Environments = new List<InitialEnvRepr>();

        /// <inheritdoc />
        public void ApplyEvent(ResolvedEvent recordedEvent, bool ignoreReplayErrors)
        {
            switch (recordedEvent.Event.EventType)
            {
                case "DefaultEnvironmentCreated":
                    ApplyDefaultEnvironmentCreated(recordedEvent, ignoreReplayErrors);
                    break;

                case "EnvironmentCreated":
                    ApplyEnvironmentCreated(recordedEvent, ignoreReplayErrors);
                    break;

                case "EnvironmentDeleted":
                    ApplyEnvironmentDeleted(recordedEvent, ignoreReplayErrors);
                    break;

                case "EnvironmentKeysModified":
                    ApplyKeysModified(recordedEvent, ignoreReplayErrors);
                    break;

                case "EnvironmentKeysImported":
                    ApplyKeysImported(recordedEvent, ignoreReplayErrors);
                    break;

                // we don't care about all other events in this Lossy format
                // we only care about the current Environments and their Data
                case "ConfigurationBuilt":
                case "StructureCreated":
                case "StructureDeleted":
                case "StructureVariablesModified":
                    break;

                default:
                    throw new MigrationReplayException($"could not handle event of type '{recordedEvent.Event.EventType}'");
            }
        }

        /// <inheritdoc />
        public List<(string Type, byte[] Data, byte[] Metadata)> GenerateEventData()
            => Environments.SelectMany(
                               e => new List<DomainEvent>
                               {
                                   e.IsDefault
                                       ? new DefaultEnvironmentCreated(new EnvironmentIdentifier(e.Identifier.Category, e.Identifier.Name))
                                       : new EnvironmentCreated(new EnvironmentIdentifier(e.Identifier.Category, e.Identifier.Name)) as DomainEvent,
                                   new EnvironmentLayerCreated(new LayerIdentifier($"ll-{e.Identifier.Category}-{e.Identifier.Name}")),
                                   new EnvironmentLayersModified(
                                       new EnvironmentIdentifier(e.Identifier.Category, e.Identifier.Name),
                                       new List<LayerIdentifier>
                                       {
                                           new LayerIdentifier($"ll-{e.Identifier.Category}-{e.Identifier.Name}")
                                       }),
                                   // don't generate Keys-Imported event when there are no keys to import
                                   // will be filtered out in the next step
                                   e.Keys.Any()
                                       ? new EnvironmentLayerKeysImported(
                                           new LayerIdentifier($"ll-{e.Identifier.Category}-{e.Identifier.Name}"),
                                           e.Keys
                                            .Select(k => ConfigKeyAction.Set(k.Key, k.Value, k.Description, k.Type))
                                            .ToArray())
                                       : null
                               })
                           .Where(e => e != null)
                           .Select(
                               e => (Type: e.EventType,
                                        Data: Encoding.UTF8.GetBytes(
                                            JsonConvert.SerializeObject(e)),
                                        Metadata: Encoding.UTF8.GetBytes(
                                            JsonConvert.SerializeObject(e.GetMetadata())
                                        )))
                           .ToList();

        private void ApplyDefaultEnvironmentCreated(ResolvedEvent resolvedEvent, bool ignoreReplayErrors)
        {
            var domainEvent = JsonConvert.DeserializeAnonymousType(
                Encoding.UTF8.GetString(resolvedEvent.Event.Data.Span),
                new {Identifier = new InitialEnvIdRepr()});

            if (Environments.Any(
                    repr => repr.Identifier.Category == domainEvent.Identifier.Category
                            && repr.Identifier.Name == domainEvent.Identifier.Name
                            && repr.IsDefault)
                && !ignoreReplayErrors)
            {
                throw new MigrationReplayException($"environment '{domainEvent.Identifier}' already created or not deleted previously");
            }

            Environments.Add(
                new InitialEnvRepr
                {
                    Identifier = domainEvent.Identifier,
                    IsDefault = true,
                    Keys = new List<InitialKeyRepr>()
                });
        }

        private void ApplyEnvironmentCreated(ResolvedEvent resolvedEvent, bool ignoreReplayErrors)
        {
            var domainEvent = JsonConvert.DeserializeAnonymousType(
                Encoding.UTF8.GetString(resolvedEvent.Event.Data.Span),
                new {Identifier = new InitialEnvIdRepr()});

            if (Environments.Any(
                    repr => repr.Identifier.Category == domainEvent.Identifier.Category
                            && repr.Identifier.Name == domainEvent.Identifier.Name
                            && !repr.IsDefault)
                && !ignoreReplayErrors)
            {
                throw new MigrationReplayException($"environment '{domainEvent.Identifier}' already created or not deleted previously");
            }

            Environments.Add(
                new InitialEnvRepr
                {
                    Identifier = domainEvent.Identifier,
                    IsDefault = false,
                    Keys = new List<InitialKeyRepr>()
                });
        }

        private void ApplyEnvironmentDeleted(ResolvedEvent resolvedEvent, bool ignoreReplayErrors)
        {
            var domainEvent = JsonConvert.DeserializeAnonymousType(
                Encoding.UTF8.GetString(resolvedEvent.Event.Data.Span),
                new {Identifier = new InitialEnvIdRepr()});

            int existingIndex = Environments.FindIndex(
                repr => repr.Identifier.Category == domainEvent.Identifier.Category
                        && repr.Identifier.Name == domainEvent.Identifier.Name
                        && !repr.IsDefault);

            if (existingIndex == -1 && !ignoreReplayErrors)
            {
                throw new MigrationReplayException($"can't find environment '{domainEvent.Identifier}' to delete, not created or already deleted");
            }

            if (existingIndex >= 0)
            {
                Environments.RemoveAt(existingIndex);
            }
        }

        /// <summary>
        ///     apply changes to the stored keys in an environment
        /// </summary>
        /// <param name="resolvedEvent">EventStore-event with associated meta-/data</param>
        /// <param name="ignoreReplayErrors">true, to ignore errors that may change the result of the replay</param>
        /// <param name="importEnvironment">create non-existent environments if necessary (Import-Behaviour)</param>
        private void ApplyKeyModifications(ResolvedEvent resolvedEvent, bool ignoreReplayErrors, bool importEnvironment)
        {
            var domainEvent = JsonConvert.DeserializeAnonymousType(
                Encoding.UTF8.GetString(resolvedEvent.Event.Data.Span),
                new
                {
                    Identifier = new InitialEnvIdRepr(),
                    ModifiedKeys = new List<InitialKeyActionRepr>()
                });

            InitialEnvRepr existing = Environments.FirstOrDefault(
                repr => repr.Identifier.Category == domainEvent.Identifier.Category
                        && repr.Identifier.Name == domainEvent.Identifier.Name);

            if (existing is null)
            {
                if (importEnvironment)
                {
                    existing = new InitialEnvRepr {Identifier = domainEvent.Identifier, Keys = new List<InitialKeyRepr>()};
                    Environments.Add(existing);
                }
                else
                {
                    if (ignoreReplayErrors)
                    {
                        return;
                    }

                    throw new MigrationReplayException(
                        $"unable to find Environment '{domainEvent.Identifier.Category}'/'{domainEvent.Identifier.Name}'");
                }
            }

            foreach (InitialKeyActionRepr action in domainEvent.ModifiedKeys.Where(a => a.Type == InitialKeyActionTypeRepr.Delete))
            {
                int existingIndex = existing.Keys.FindIndex(e => e.Key.Equals(action.Key, StringComparison.OrdinalIgnoreCase));

                if (existingIndex >= 0)
                {
                    existing.Keys.RemoveAt(existingIndex);
                }
            }

            foreach (InitialKeyActionRepr action in domainEvent.ModifiedKeys.Where(a => a.Type == InitialKeyActionTypeRepr.Set))
            {
                int existingIndex = existing.Keys.FindIndex(e => e.Key.Equals(action.Key, StringComparison.OrdinalIgnoreCase));

                if (existingIndex >= 0)
                {
                    existing.Keys[existingIndex] = new InitialKeyRepr
                    {
                        Key = action.Key,
                        Value = action.Value,
                        Description = action.Description,
                        Type = action.ValueType
                    };
                }
                else
                {
                    existing.Keys.Add(
                        new InitialKeyRepr
                        {
                            Key = action.Key,
                            Value = action.Value,
                            Description = action.Description,
                            Type = action.ValueType
                        });
                }
            }
        }

        private void ApplyKeysImported(ResolvedEvent resolvedEvent, bool ignoreReplayErrors) =>
            ApplyKeyModifications(resolvedEvent, ignoreReplayErrors, true);

        private void ApplyKeysModified(ResolvedEvent resolvedEvent, bool ignoreReplayErrors) =>
            ApplyKeyModifications(resolvedEvent, ignoreReplayErrors, false);
    }
}
