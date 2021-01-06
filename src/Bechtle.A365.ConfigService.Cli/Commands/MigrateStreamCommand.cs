using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using EventStore.ClientAPI;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;

namespace Bechtle.A365.ConfigService.Cli.Commands
{
    [Command("stream", Description = "migrate the EventStore-Stream from one to another version")]
    public class MigrateStreamCommand : SubCommand<MigrateCommand>
    {
        public enum MigrationAccuracy
        {
            /// <summary>
            ///     only the latest relevant state is migrated from one version to another.
            ///     certain Events will be cut from the migrated stream, because it is expected to be recreated (Structures / Configurations).
            /// </summary>
            Lossy,

            /// <summary>
            ///     the complete state will be migrated from one version to another, re-creating all events with equivalent events of the newer version
            /// </summary>
            Lossless
        }

        /// <summary>
        ///     Named versions of EventStore-Streams
        /// </summary>
        public enum StreamVersion
        {
            Undefined = 0,
            Initial = 1,
            LayeredEnvironments = 2
        }

        /// <inheritdoc />
        public MigrateStreamCommand(IConsole console) : base(console)
        {
        }

        [Option("-a|--accuracy", "Accuracy with which the Stream will be migrated from one version to another\r\n" +
                                 "'Lossy': only the latest relevant state is migrated.\r\n" +
                                 "'Lossless': the complete state will be migrated from one version to another, re-creating all events with equivalent events of the newer version",
                CommandOptionType.SingleValue)]
        public MigrationAccuracy Accuracy { get; set; } = MigrationAccuracy.Lossless;

        [Option("-b|--batch-size",
                "number of events to fetch in one read-operation. larger values will improve throughput but may cause timeouts on some clusters / nodes",
                CommandOptionType.SingleValue)]
        public int EventStoreBatchSize { get; set; } = 16;

        [Option("-c|--connection-string", "connection-string to use when connecting to EventStore", CommandOptionType.SingleValue)]
        public string EventStoreConnectionString { get; set; } = string.Empty;

        [Option("-n|--stream", "Stream to migrate to the new Version", CommandOptionType.SingleValue)]
        public string EventStoreStream { get; set; } = string.Empty;

        [Option("-f|--from", "current version of the targeted EventStore-Stream", CommandOptionType.SingleValue)]
        public StreamVersion From { get; set; } = StreamVersion.Undefined;

        [Option("--ignore-replay-errors",
                "ignore non-critical errors that might change the migration-result. this might be necessary to force migrations of faulty streams",
                CommandOptionType.SingleOrNoValue)]
        public bool IgnoreReplayErrors { get; set; } = false;

        [Option("-t|--to", "target version of the targeted EventStore-Stream", CommandOptionType.SingleValue)]
        public StreamVersion To { get; set; } = StreamVersion.Undefined;

        [Option("-l|--local", "use the previously generated local 'migrated-state.json' for the migration. " +
                              "this needs to be set in case the migration has previously failed while writing new events to the stream",
                CommandOptionType.NoValue)]
        public bool UseLocalData { get; set; } = false;

        /// <inheritdoc />
        protected override bool CheckParameters()
        {
            if (!base.CheckParameters())
                return false;

            if (string.IsNullOrWhiteSpace(EventStoreConnectionString))
            {
                Output.WriteError("parameter (-c|--connection-string) must not be empty -- see help for more information");
                return false;
            }

            if (string.IsNullOrWhiteSpace(EventStoreStream))
            {
                Output.WriteError("parameter (-s|--stream) must not be empty -- see help for more information");
                return false;
            }

            switch (Accuracy)
            {
                case MigrationAccuracy.Lossy:
                case MigrationAccuracy.Lossless:
                    break;

                default:
                {
                    Output.Write("unknown (-a|--accuracy) option given -- see help for more information");
                    return false;
                }
            }

            switch (From)
            {
                case StreamVersion.Undefined:
                    Output.WriteError("no (-f|--from) option given -- see help for more information");
                    return false;

                case StreamVersion.Initial:
                case StreamVersion.LayeredEnvironments:
                    break;

                default:
                    Output.WriteError("unknown (-f|--from) option given -- see help for more information");
                    return false;
            }

            switch (To)
            {
                case StreamVersion.Undefined:
                    Output.WriteError("no (-t|--to) option given -- see help for more information");
                    return false;

                case StreamVersion.Initial:
                case StreamVersion.LayeredEnvironments:
                    break;

                default:
                    Output.WriteError("unknown (-t|--to) option given -- see help for more information");
                    return false;
            }

            if (From >= To)
            {
                Output.WriteError("(-f|--from) cannot be greater than or equal to (-t|--to)");
                return false;
            }

            return true;
        }

        /// <inheritdoc />
        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            // @TODO: once multiple stream-versions are supported, add a smarter way to migrate from version to version
            // ATM we only support Initial => LayeredEnvironments
            // because the following combinations are already filtered out by CheckParameters, we only need to see if this one specific combination is given
            // Undefined => [Anything]                    (Undefined is not possible)
            // [Anything] => Undefined                    (Undefined is not possible)
            // Initial => Initial                         (cannot be equal)
            // LayeredEnvironments => LayeredEnvironments (cannot be equal)
            // LayeredEnvironments => Initial             (cannot be smaller)

            if (From == StreamVersion.Initial && To == StreamVersion.LayeredEnvironments)
                switch (Accuracy)
                {
                    case MigrationAccuracy.Lossy:
                        try
                        {
                            return await LossyMigrationInitialToLayeredEnvironments();
                        }
                        catch (Exception e)
                        {
                            Output.WriteError($"error while executing migration (Initial => LayeredEnvironments | Lossy): {e}");
                            return 1;
                        }
                    case MigrationAccuracy.Lossless:
                    {
                        Output.Write("Lossless migration (Initial => LayeredEnvironments) is currently not supported.");
                        return 1;
                    }
                }

            return await base.OnExecute(app);
        }

        private async Task<IEventStoreConnection> ConnectToEventStore()
        {
            var eventStore = EventStoreConnection.Create(
                $"ConnectTo={EventStoreConnectionString}",
                ConnectionSettings.Create()
                                  .PerformOnMasterOnly()
                                  .KeepReconnecting()
                                  .LimitRetriesForOperationTo(10)
                                  .LimitConcurrentOperationsTo(1)
                                  .SetOperationTimeoutTo(TimeSpan.FromSeconds(30))
                                  .SetReconnectionDelayTo(TimeSpan.FromSeconds(1))
                                  .SetHeartbeatTimeout(TimeSpan.FromSeconds(30))
                                  .SetHeartbeatInterval(TimeSpan.FromSeconds(60)),
                $"StreamMigration-{Environment.UserDomainName}\\{Environment.UserName}@{Environment.MachineName}");

            await eventStore.ConnectAsync();

            return eventStore;
        }

        /// <summary>
        ///     delete stream (<see cref="EventStoreStream" />) and recreate it with the given list of events
        /// </summary>
        /// <param name="lastEventNumber">expected event-version, deletion will fail if this doesn't match actual last-version</param>
        /// <param name="events">list of Domain-Events written to EventStore</param>
        /// <param name="eventStore">opened connection to EventStore</param>
        /// <returns>exit-code 0=Success | 1=Failure</returns>
        private async Task<int> DeleteAndRecreateStream(long lastEventNumber, List<DomainEvent> events, IEventStoreConnection eventStore)
        {
            if (events.Count == 0)
            {
                Output.WriteError("no events could be generated from current state. \r\n" +
                                  "no data would be migrated during this operation. \r\n" +
                                  "delete stream manually (Soft-Delete / TruncateBefore - NO HARD DELETE / TombStone), it will be automagically recreated by ConfigService on the first write.");
                return 1;
            }

            Output.WriteVerboseLine($"deleting stream '{EventStoreStream}', expected to be at position '{lastEventNumber}'");
            await eventStore.DeleteStreamAsync(EventStoreStream, lastEventNumber);

            Output.WriteVerboseLine($"stream deleted, recreating it with '{events.Count}' new events");
            using var transaction = await eventStore.StartTransactionAsync(EventStoreStream, ExpectedVersion.NoStream);

            try
            {
                foreach (var @event in events)
                {
                    Output.WriteVerboseLine($"writing event '{@event.EventType}'");
                    var data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(@event));
                    var metadata = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(@event.GetMetadata()));

                    await transaction.WriteAsync(new EventData(Guid.NewGuid(), @event.EventType, true, data, metadata));
                }

                Output.WriteVerboseLine("all events have been written, committing transaction");
                await transaction.CommitAsync();
                return 0;
            }
            catch (Exception e)
            {
                Output.WriteError($"error while writing events to stream '{EventStoreStream}', aborting transaction: {e}");
                transaction.Rollback();
                return 1;
            }
        }

        /// <summary>
        ///     record initial state and write it to the given file
        /// </summary>
        /// <param name="eventStore">opened connection to EventStore</param>
        /// <param name="backupFileLocation">path to file in which state is backed-up into</param>
        /// <returns>exit-code, 0=success | 1=failure</returns>
        private async Task<int> GenerateLossyInitialState(IEventStoreConnection eventStore, string backupFileLocation)
        {
            Output.WriteVerboseLine("reading relevant current state from EventStore");

            try
            {
                var currentState = await RecordInitialState(eventStore);

                if (currentState is null)
                    return 1;

                var json = JsonConvert.SerializeObject(currentState, new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented
                });

                if (File.Exists(backupFileLocation))
                {
                    Output.WriteError($"could not save backup-file to '{backupFileLocation}' - overwriting this file is not intended. \r\n" +
                                      "if you want to use a previously-generated backup use the (-l|--local) flag to skip generating a new backup");
                    return 1;
                }

                await File.WriteAllTextAsync(backupFileLocation, json, Encoding.UTF8);

                return 0;
            }
            catch (JsonException e)
            {
                Output.WriteError($"could not generate backup json-file before beginning the actual migration: {e}");
                return 1;
            }
            catch (IOException e)
            {
                Output.WriteError($"could not save migrated state to local disk before beginning actual migration: {e}");
                return 1;
            }
        }

        /// <summary>
        ///     read the configured Stream (<see cref="EventStoreStream" />), create a local backup,
        ///     delete and then re-create the stream with the equivalent events.
        ///     this migration is lossy, and does not preserve events for (Structures, Environment-Modifications, Configurations).
        ///     this migration only preserves the last state of all existing Environments.
        /// </summary>
        /// <returns>exit-code, 0=success | 1=failure</returns>
        private async Task<int> LossyMigrationInitialToLayeredEnvironments()
        {
            var backupFileLocation = "./migrated-state.json";

            var eventStore = await ConnectToEventStore();

            Output.WriteVerboseLine("getting last event# for later");

            var lastEventNumber = (await eventStore.ReadStreamEventsBackwardAsync(EventStoreStream, StreamPosition.End, 1, true))?.LastEventNumber ?? -1;
            if (lastEventNumber == -1)
            {
                Output.WriteError($"could not determine last event in the stream '{EventStoreStream}', won't be able to delete stream later on");
                return 1;
            }

            Output.WriteVerboseLine($"last event#={lastEventNumber}");

            if (!UseLocalData)
            {
                var result = await GenerateLossyInitialState(eventStore, backupFileLocation);
                if (result != 0)
                    return result;
            }

            LossyInitialRecordedRepository state;

            if (File.Exists(backupFileLocation))
            {
                Output.WriteVerboseLine("reading state from generated backup-file");
                var json = await File.ReadAllTextAsync(backupFileLocation, Encoding.UTF8);

                try
                {
                    state = JsonConvert.DeserializeObject<LossyInitialRecordedRepository>(json);
                }
                catch (JsonException e)
                {
                    Output.WriteError($"could not read state from generated backup: {e}");
                    return 1;
                }
            }
            else
            {
                Output.WriteError("could not read state from generated backup: file not found");
                return 1;
            }

            var events = state.GenerateDomainEvents();

            return await DeleteAndRecreateStream(lastEventNumber, events, eventStore);
        }

        /// <summary>
        ///     read through all events in the stream (<see cref="EventStoreStream" />) and take all relevant data.
        /// </summary>
        /// <param name="eventStore">opened EventStore-Connection</param>
        /// <returns>object containing all relevant state for the Initial-Version of this Stream</returns>
        private async Task<LossyInitialRecordedRepository> RecordInitialState(IEventStoreConnection eventStore)
        {
            var currentState = new LossyInitialRecordedRepository();

            long currentPosition = StreamPosition.Start;
            bool continueReading;

            do
            {
                var slice = await eventStore.ReadStreamEventsForwardAsync(EventStoreStream, currentPosition, EventStoreBatchSize, true);

                Output.WriteVerboseLine($"read '{slice.Events.Length}' events {slice.FromEventNumber}-{slice.NextEventNumber - 1}/{slice.LastEventNumber}");

                foreach (var recordedEvent in slice.Events)
                {
                    Output.WriteVerboseLine($"applying event '{recordedEvent.Event.EventStreamId}#{recordedEvent.Event.EventNumber}'");

                    try
                    {
                        currentState.ApplyEvent(recordedEvent, IgnoreReplayErrors);
                    }
                    catch (MigrationReplayException e)
                    {
                        Output.WriteErrorLine($"can't apply event '{recordedEvent.Event.EventStreamId}#{recordedEvent.Event.EventNumber}': {e}");
                        return null;
                    }
                }

                currentPosition = slice.NextEventNumber;
                continueReading = !slice.IsEndOfStream;
            } while (continueReading);

            return currentState;
        }

        /// <summary>
        ///     type of action to apply to a key
        /// </summary>
        private enum InitialKeyActionTypeRepr
        {
            /// <summary>
            ///     add / update the value of the key
            /// </summary>
            Set,

            /// <summary>
            ///     remove the key
            /// </summary>
            Delete
        }

        /// <summary>
        ///     Records and represents the total state of a ConfigService-EventStream.
        ///     Uses this information to generate the equivalent state in the migrated form.
        /// </summary>
        private class LossyInitialRecordedRepository
        {
            public readonly List<InitialEnvRepr> Environments = new List<InitialEnvRepr>();

            /// <summary>
            ///     take the Domain-Event and apply its changes.
            /// </summary>
            /// <param name="recordedEvent">some recorded event from the EventStore</param>
            /// <param name="ignoreReplayErrors">flag indicating if replay-errors should be ignored or throw an <see cref="MigrationReplayException" /></param>
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

            /// <summary>
            ///     take the accumulated state (<see cref="ApplyEvent" />) and generate events that translate this state into the equivalent state for <see cref="StreamVersion.LayeredEnvironments" />
            /// </summary>
            /// <returns>unordered list of domain-events</returns>
            public List<DomainEvent> GenerateDomainEvents()
                => Environments.SelectMany(e => new List<DomainEvent>
                                {
                                    e.IsDefault
                                        ? new DefaultEnvironmentCreated(new EnvironmentIdentifier(e.Identifier.Category, e.Identifier.Name))
                                        : new EnvironmentCreated(new EnvironmentIdentifier(e.Identifier.Category, e.Identifier.Name)) as DomainEvent,
                                    new EnvironmentLayerCreated(new LayerIdentifier($"ll-{e.Identifier.Category}-{e.Identifier.Name}")),
                                    new EnvironmentLayersModified(new EnvironmentIdentifier(e.Identifier.Category, e.Identifier.Name),
                                                                  new List<LayerIdentifier>
                                                                  {
                                                                      new LayerIdentifier($"ll-{e.Identifier.Category}-{e.Identifier.Name}")
                                                                  }),
                                    // don't generate Keys-Imported event when there are no keys to import
                                    // will be filtered out in the next step
                                    e.Keys.Any()
                                        ? new EnvironmentLayerKeysImported(new LayerIdentifier($"ll-{e.Identifier.Category}-{e.Identifier.Name}"),
                                                                           e.Keys
                                                                            .Select(k => ConfigKeyAction.Set(k.Key, k.Value, k.Description, k.Type))
                                                                            .ToArray())
                                        : null
                                })
                                .Where(e => e != null)
                                .ToList();

            private void ApplyDefaultEnvironmentCreated(ResolvedEvent resolvedEvent, bool ignoreReplayErrors)
            {
                var domainEvent = JsonConvert.DeserializeAnonymousType(Encoding.UTF8.GetString(resolvedEvent.Event.Data),
                                                                       new {Identifier = new InitialEnvIdRepr()});

                if (Environments.Any(repr => repr.Identifier.Category == domainEvent.Identifier.Category
                                              && repr.Identifier.Name == domainEvent.Identifier.Name
                                              && repr.IsDefault)
                    && !ignoreReplayErrors)
                    throw new MigrationReplayException($"environment '{domainEvent.Identifier}' already created or not deleted previously");

                Environments.Add(new InitialEnvRepr
                {
                    Identifier = domainEvent.Identifier,
                    IsDefault = true,
                    Keys = new List<InitialKeyRepr>()
                });
            }

            private void ApplyEnvironmentCreated(ResolvedEvent resolvedEvent, bool ignoreReplayErrors)
            {
                var domainEvent = JsonConvert.DeserializeAnonymousType(Encoding.UTF8.GetString(resolvedEvent.Event.Data),
                                                                       new {Identifier = new InitialEnvIdRepr()});

                if (Environments.Any(repr => repr.Identifier.Category == domainEvent.Identifier.Category
                                              && repr.Identifier.Name == domainEvent.Identifier.Name
                                              && !repr.IsDefault)
                    && !ignoreReplayErrors)
                    throw new MigrationReplayException($"environment '{domainEvent.Identifier}' already created or not deleted previously");

                Environments.Add(new InitialEnvRepr
                {
                    Identifier = domainEvent.Identifier,
                    IsDefault = false,
                    Keys = new List<InitialKeyRepr>()
                });
            }

            private void ApplyEnvironmentDeleted(ResolvedEvent resolvedEvent, bool ignoreReplayErrors)
            {
                var domainEvent = JsonConvert.DeserializeAnonymousType(Encoding.UTF8.GetString(resolvedEvent.Event.Data),
                                                                       new {Identifier = new InitialEnvIdRepr()});

                var existingIndex = Environments.FindIndex(repr => repr.Identifier.Category == domainEvent.Identifier.Category
                                                                    && repr.Identifier.Name == domainEvent.Identifier.Name
                                                                    && !repr.IsDefault);

                if (existingIndex == -1 && !ignoreReplayErrors)
                    throw new MigrationReplayException($"can't find environment '{domainEvent.Identifier}' to delete, not created or already deleted");

                if (existingIndex >= 0)
                    Environments.RemoveAt(existingIndex);
            }

            /// <summary>
            ///     apply changes to the stored keys in an environment
            /// </summary>
            /// <param name="resolvedEvent">EventStore-event with associated meta-/data</param>
            /// <param name="ignoreReplayErrors">true, to ignore errors that may change the result of the replay</param>
            /// <param name="importEnvironment">create non-existent environments if necessary (Import-Behaviour)</param>
            private void ApplyKeyModifications(ResolvedEvent resolvedEvent, bool ignoreReplayErrors, bool importEnvironment)
            {
                var domainEvent = JsonConvert.DeserializeAnonymousType(Encoding.UTF8.GetString(resolvedEvent.Event.Data),
                                                                       new
                                                                       {
                                                                           Identifier = new InitialEnvIdRepr(),
                                                                           ModifiedKeys = new List<InitialKeyActionRepr>()
                                                                       });

                var existing = Environments.FirstOrDefault(repr => repr.Identifier.Category == domainEvent.Identifier.Category
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
                            return;

                        throw new MigrationReplayException(
                            $"unable to find Environment '{domainEvent.Identifier.Category}'/'{domainEvent.Identifier.Name}'");
                    }
                }

                foreach (var action in domainEvent.ModifiedKeys.Where(a => a.Type == InitialKeyActionTypeRepr.Delete))
                {
                    var existingIndex = existing.Keys.FindIndex(e => e.Key.Equals(action.Key, StringComparison.OrdinalIgnoreCase));

                    if (existingIndex >= 0)
                        existing.Keys.RemoveAt(existingIndex);
                }

                foreach (var action in domainEvent.ModifiedKeys.Where(a => a.Type == InitialKeyActionTypeRepr.Set))
                {
                    var existingIndex = existing.Keys.FindIndex(e => e.Key.Equals(action.Key, StringComparison.OrdinalIgnoreCase));

                    if (existingIndex >= 0)
                        existing.Keys[existingIndex] = new InitialKeyRepr
                        {
                            Key = action.Key,
                            Value = action.Value,
                            Description = action.Description,
                            Type = action.ValueType
                        };
                    else
                        existing.Keys.Add(new InitialKeyRepr
                        {
                            Key = action.Key,
                            Value = action.Value,
                            Description = action.Description,
                            Type = action.ValueType
                        });
                }
            }

            private void ApplyKeysImported(ResolvedEvent resolvedEvent, bool ignoreReplayErrors) =>
                ApplyKeyModifications(resolvedEvent, ignoreReplayErrors, true);

            private void ApplyKeysModified(ResolvedEvent resolvedEvent, bool ignoreReplayErrors) =>
                ApplyKeyModifications(resolvedEvent, ignoreReplayErrors, false);
        }

// all structs / fields are being used and assigned, but only while deserializing from JSON
#pragma warning disable S3459 // Unassigned members should be removed
#pragma warning disable 649
        /// <summary>
        ///     internal representation of an Environment-Id in its 'Initial' version
        /// </summary>
        private struct InitialEnvIdRepr
        {
            public string Category;

            public string Name;
        }

        /// <summary>
        ///     internal representation of an Environment in its 'Initial' version
        /// </summary>
        private class InitialEnvRepr
        {
            public InitialEnvIdRepr Identifier;

            public bool IsDefault;

            public List<InitialKeyRepr> Keys;
        }

        /// <summary>
        ///     internal representation of an Environment-Key in its 'Initial' version
        /// </summary>
        private struct InitialKeyRepr
        {
            public string Key;

            public string Value;

            public string Description;

            public string Type;
        }

        /// <summary>
        ///     internal representation of an Environment-Key-Action in its 'Initial' version
        /// </summary>
        private struct InitialKeyActionRepr
        {
            public string Key;

            public string Value;

            public string Description;

            public string ValueType;

            public InitialKeyActionTypeRepr Type;
        }

        /// <summary>
        ///     Exception indicating that some invalid operation occurred in the EventStream, which may alter the result of the Migration.
        /// </summary>
        [Serializable]
        private class MigrationReplayException : Exception
        {
            /// <inheritdoc />
            public MigrationReplayException(string message) : base(message)
            {
            }

            /// <inheritdoc />
            protected MigrationReplayException(SerializationInfo info, StreamingContext context) : base(info, context)
            {
            }
        }
#pragma warning restore 649
#pragma warning restore S3459 // Unassigned members should be removed
// all structs / fields are being used and assigned, but only while deserializing from JSON
    }
}