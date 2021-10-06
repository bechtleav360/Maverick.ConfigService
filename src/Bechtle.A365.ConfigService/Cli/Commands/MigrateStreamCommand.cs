using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Cli.Commands.MigrationModels;
using Bechtle.A365.ServiceBase.Commands;
using EventStore.Client;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using StreamPosition = EventStore.Client.StreamPosition;

namespace Bechtle.A365.ConfigService.Cli.Commands
{
    /// <summary>
    ///     Migrate ConfigStream-Data from one version to another
    /// </summary>
    [Command("stream", Description = "migrate the EventStore-Stream from one to another version")]
    public class MigrateStreamCommand : SubCommand<MigrateCommand>
    {
        /// <summary>
        ///     Explicit option to set the desired Accuracy.
        ///     Most/All migrations are only available in the Lossy-Flavour,
        ///     but we want the user to acknowledge that the migration is Lossy be providing the Option
        /// </summary>
        [Option(
            "-a|--accuracy",
            "Accuracy with which the Stream will be migrated from one version to another\r\n"
            + "'Lossy': only the latest relevant state is migrated.\r\n"
            + "'Lossless': the complete state will be migrated from one version to another, re-creating all events with equivalent events of the newer version",
            CommandOptionType.SingleValue)]
        public MigrationAccuracy Accuracy { get; set; } = MigrationAccuracy.Lossless;

        /// <summary>
        ///     ConnectionString with which to connect directly to the EventStore
        /// </summary>
        [Option("-c|--connection-string", "connection-string to use when connecting to EventStore", CommandOptionType.SingleValue)]
        public string EventStoreConnectionString { get; set; } = string.Empty;

        /// <summary>
        ///     Name of the Stream to migrate
        /// </summary>
        [Option("-n|--stream", "Stream to migrate to the new Version", CommandOptionType.SingleValue)]
        public string EventStoreStream { get; set; } = string.Empty;

        /// <summary>
        ///     Version the data inside the Stream is currently at
        /// </summary>
        [Option(
            "-f|--from",
            "current version of the targeted EventStore-Stream\r\n"
            + "'Initial': First rewritten version of ConfigService where Environments contained Data\r\n"
            + "'LayeredEnvironments': Version of ConfigService where Environments were split into composable Layers that contained Data\r\n"
            + "'ServiceBased': Rewrite of ConfigService that wrapped DomainEvents in another abstraction-layer",
            CommandOptionType.SingleValue)]
        public StreamVersion From { get; set; } = StreamVersion.Undefined;

        /// <summary>
        ///     Flag telling the CLI to ignore errors during migration.
        ///     This might be necessary when some Events contain now-invalid data and can prevent migration
        /// </summary>
        [Option(
            "--ignore-replay-errors",
            "ignore non-critical errors that might change the migration-result. this might be necessary to force migrations of faulty streams",
            CommandOptionType.SingleOrNoValue)]
        public bool IgnoreReplayErrors { get; set; } = false;

        /// <summary>
        ///     Version the data inside the Stream should be migrated to
        /// </summary>
        [Option(
            "-t|--to",
            "target version of the targeted EventStore-Stream\r\n"
            + "'Initial': First rewritten version of ConfigService where Environments contained Data\r\n"
            + "'LayeredEnvironments': Version of ConfigService where Environments were split into composable Layers that contained Data\r\n"
            + "'ServiceBased': Rewrite of ConfigService that wrapped DomainEvents in another abstraction-layer",
            CommandOptionType.SingleValue)]
        public StreamVersion To { get; set; } = StreamVersion.Undefined;

        /// <summary>
        ///     Use the generated State from a previous run.
        ///     This needs to be explicitly set to avoid accidentally overriding files
        /// </summary>
        [Option(
            "-l|--local",
            "use the previously generated local 'migrated-state.json' for the migration. "
            + "this needs to be set in case the migration has previously failed while writing new events to the stream",
            CommandOptionType.NoValue)]
        public bool UseLocalData { get; set; } = false;

        /// <summary>
        ///     Accuracy with which the app should migrate the data
        /// </summary>
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
            /// <summary>
            ///     Version is not defined - default-value to catch errors
            /// </summary>
            Undefined = 0,

            /// <summary>
            ///     First version of DomainEvents that can be migrated.
            ///     This is the Version where Environments were modified directly
            /// </summary>
            Initial = 1,

            /// <summary>
            ///     Second version of DomainEvents.
            ///     This is the version that split Environments into Layers that can be assigned and modified
            /// </summary>
            LayeredEnvironments = 2,

            /// <summary>
            ///     Third version of DomainEvents.
            ///     This is the version that wrapped all DomainEvents in the IDomainEvent shell provided by ServiceBase.
            /// </summary>
            ServiceBased = 3
        }

        /// <inheritdoc />
        public MigrateStreamCommand(IOutput output) : base(output)
        {
        }

        /// <inheritdoc />
        protected override bool CheckParameters()
        {
            if (!base.CheckParameters())
            {
                return false;
            }

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
        protected override async Task<int> OnExecuteAsync(CommandLineApplication app)
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
            {
                switch (Accuracy)
                {
                    case MigrationAccuracy.Lossy:
                        try
                        {
                            return await LossyMigration(e => RecordState<LossyInitialRecordedRepository>(e));
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
            }

            if (From == StreamVersion.LayeredEnvironments && To == StreamVersion.ServiceBased)
            {
                switch (Accuracy)
                {
                    case MigrationAccuracy.Lossy:
                        try
                        {
                            return await LossyMigration(
                                       e => RecordState<LossyLayeredEnvironmentRepository>(
                                           e,
                                           s => s.EventStoreConnectionString = EventStoreConnectionString));
                        }
                        catch (Exception e)
                        {
                            Output.WriteError($"error while executing migration (LayeredEnvironments => ServiceBased | Lossy): {e}");
                            return 1;
                        }
                    case MigrationAccuracy.Lossless:
                    {
                        Output.Write("Lossless migration (LayeredEnvironments => ServiceBased) is currently not supported.");
                        return 1;
                    }
                }
            }

            app.ShowHelp();
            return 0;
        }

        /// <summary>
        ///     delete stream (<see cref="EventStoreStream" />) and recreate it with the given list of events
        /// </summary>
        /// <param name="streamPosition">expected event-version, deletion will fail if this doesn't match actual last-version</param>
        /// <param name="events">list of Domain-Events written to EventStore</param>
        /// <param name="eventStore">opened connection to EventStore</param>
        /// <returns>exit-code 0=Success | 1=Failure</returns>
        private async Task<int> DeleteAndRecreateStream(StreamPosition streamPosition, IList<(string, byte[], byte[])> events, EventStoreClient eventStore)
        {
            if (events.Count == 0)
            {
                Output.WriteError(
                    "no events could be generated from current state. \r\n"
                    + "no data would be migrated during this operation. \r\n"
                    + "delete stream manually (Soft-Delete / TruncateBefore - NO HARD DELETE / TombStone), it will be automagically recreated by ConfigService on the first write.");
                return 1;
            }

            Output.WriteVerboseLine($"deleting stream '{EventStoreStream}', expected to be at position '{streamPosition}'");
            await eventStore.SoftDeleteAsync(EventStoreStream, StreamRevision.FromStreamPosition(streamPosition));

            Output.WriteVerboseLine($"stream deleted, recreating it with '{events.Count}' new events");

            try
            {
                StreamRevision lastPosition = StreamRevision.None;
                foreach ((string type, byte[] data, byte[] metadata) in events)
                {
                    Output.WriteVerboseLine($"writing event {lastPosition} '{type}', data={data.Length}b, metadata={metadata.Length}b");

                    var @event = new EventData(Uuid.NewUuid(), type, data, metadata);
                    IWriteResult result = await eventStore.AppendToStreamAsync(EventStoreStream, lastPosition, new[] { @event });

                    lastPosition = result.NextExpectedStreamRevision;
                }

                return 0;
            }
            catch (Exception e)
            {
                Output.WriteError($"error while writing events to stream '{EventStoreStream}', aborting transaction: {e}");
                return 1;
            }
        }

        /// <summary>
        ///     record some state and write it to the given file
        /// </summary>
        /// <param name="eventStore">opened connection to EventStore</param>
        /// <param name="backupFileLocation">path to file in which state is backed-up into</param>
        /// <param name="stateGenerator">function that generates some state that can be stored</param>
        /// <returns>exit-code, 0=success | 1=failure</returns>
        private async Task<int> GenerateState<TState>(
            EventStoreClient eventStore,
            string backupFileLocation,
            Func<EventStoreClient, Task<TState?>> stateGenerator)
        {
            Output.WriteVerboseLine("reading relevant current state from EventStore");

            try
            {
                if (await stateGenerator(eventStore) is not { } currentState)
                {
                    return -1;
                }

                string json = JsonConvert.SerializeObject(
                    currentState,
                    new JsonSerializerSettings
                    {
                        Formatting = Formatting.Indented,
                        TypeNameHandling = TypeNameHandling.All
                    });

                if (File.Exists(backupFileLocation))
                {
                    Output.WriteError(
                        $"could not save backup-file to '{backupFileLocation}' - overwriting this file is not intended. \r\n"
                        + "if you want to use a previously-generated backup use the (-l|--local) flag to skip generating a new backup");
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
        private async Task<int> LossyMigration<TState>(Func<EventStoreClient, Task<TState?>> stateGenerator)
            where TState : IState
        {
            var backupFileLocation = "./migrated-state.json";

            var settings = EventStoreClientSettings.Create(EventStoreConnectionString);
            settings.ConnectionName = $"StreamMigration-{Environment.UserDomainName}\\{Environment.UserName}@{Environment.MachineName}";
            settings.ConnectivitySettings.NodePreference = NodePreference.Random;
            settings.OperationOptions.TimeoutAfter = TimeSpan.FromMinutes(1);
            var eventStore = new EventStoreClient(settings);

            Output.WriteVerboseLine("getting last event# for later");

            EventStoreClient.ReadStreamResult stream = eventStore.ReadStreamAsync(
                Direction.Backwards,
                EventStoreStream,
                StreamPosition.End,
                1,
                resolveLinkTos: true);
            StreamPosition position = (await stream.FirstOrDefaultAsync()).Event.EventNumber;
            if (position == default)
            {
                Output.WriteError($"could not determine last event in the stream '{EventStoreStream}', won't be able to delete stream later on");
                return 1;
            }

            Output.WriteVerboseLine($"last event#={position}");

            if (!UseLocalData)
            {
                int result = await GenerateState(eventStore, backupFileLocation, stateGenerator);
                if (result != 0)
                {
                    return result;
                }
            }

            TState state;

            if (File.Exists(backupFileLocation))
            {
                Output.WriteVerboseLine("reading state from generated backup-file");
                string json = await File.ReadAllTextAsync(backupFileLocation, Encoding.UTF8);

                try
                {
                    if (JsonConvert.DeserializeObject<TState>(
                            json,
                            new JsonSerializerSettings
                            {
                                TypeNameHandling = TypeNameHandling.All
                            }) is { } deserialized)
                    {
                        state = deserialized;
                    }
                    else
                    {
                        Output.WriteError("could not deserialize backup into object");
                        return 1;
                    }
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

            List<(string Type, byte[] Data, byte[] Metadata)> events = state.GenerateEventData();

            return await DeleteAndRecreateStream(position, events, eventStore);
        }

        /// <summary>
        ///     read through all events in the stream (<see cref="EventStoreStream" />) and take all relevant data.
        /// </summary>
        /// <param name="eventStore">opened EventStore-Connection</param>
        /// <param name="stateCustomizer">action to customize the state before applying events to it</param>
        /// <returns>object containing all relevant state for the Initial-Version of this Stream</returns>
        private async Task<TState?> RecordState<TState>(EventStoreClient eventStore, Action<TState>? stateCustomizer = null)
            where TState : class, IState, new()
        {
            var currentState = new TState();
            stateCustomizer?.Invoke(currentState);

            await foreach (ResolvedEvent resolvedEvent in eventStore.ReadStreamAsync(
                Direction.Forwards,
                EventStoreStream,
                StreamPosition.Start,
                resolveLinkTos: true))
            {
                Output.WriteVerboseLine($"applying event '{resolvedEvent.Event.EventStreamId}#{resolvedEvent.Event.EventNumber}'");

                try
                {
                    currentState.ApplyEvent(resolvedEvent, IgnoreReplayErrors);
                }
                catch (MigrationReplayException e)
                {
                    Output.WriteErrorLine($"can't apply event '{resolvedEvent.Event.EventStreamId}#{resolvedEvent.Event.EventNumber}': {e}");
                    return null;
                }
            }

            return currentState;
        }
    }
}
