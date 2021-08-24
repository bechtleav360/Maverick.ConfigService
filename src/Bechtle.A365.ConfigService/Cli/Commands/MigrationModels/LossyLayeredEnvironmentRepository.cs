using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ServiceBase.EventStore.Abstractions;
using Bechtle.A365.ServiceBase.EventStore.DomainEventBase;
using EventStore.Client;
using Newtonsoft.Json;

namespace Bechtle.A365.ConfigService.Cli.Commands.MigrationModels
{
    /// <summary>
    ///     Lossy representation of what data is stored in the LayeredEnvironment-Version of the EventStore
    /// </summary>
    public class LossyLayeredEnvironmentRepository : IState
    {
        private List<OptionEntry> _options;

        /// <summary>
        ///     Connection-String used to retrieve information about the maximum size of an Event
        /// </summary>
        public string EventStoreConnectionString;

        // public so it can be properly de-/serialized
        /// <summary>
        ///     List of DomainEvents stored in this State
        /// </summary>
        public readonly List<IDomainEvent> RecordedDomainEvents = new List<IDomainEvent>();

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
                    WrapDomainEvent<DefaultEnvironmentCreated>(recordedEvent, ignoreReplayErrors);
                    break;

                case "EnvironmentCreated":
                    WrapDomainEvent<EnvironmentCreated>(recordedEvent, ignoreReplayErrors);
                    break;

                case "EnvironmentDeleted":
                    WrapDomainEvent<EnvironmentDeleted>(recordedEvent, ignoreReplayErrors);
                    break;

                case "EnvironmentLayerCreated":
                    WrapDomainEvent<EnvironmentLayerCreated>(recordedEvent, ignoreReplayErrors);
                    break;

                case "EnvironmentLayerDeleted":
                    WrapDomainEvent<EnvironmentLayerDeleted>(recordedEvent, ignoreReplayErrors);
                    break;

                case "EnvironmentLayerKeysImported":
                    WrapDomainEvent<EnvironmentLayerKeysImported>(recordedEvent, ignoreReplayErrors);
                    break;

                case "EnvironmentLayerKeysModified":
                    WrapDomainEvent<EnvironmentLayerKeysModified>(recordedEvent, ignoreReplayErrors);
                    break;

                case "EnvironmentLayersModified":
                    WrapDomainEvent<EnvironmentLayersModified>(recordedEvent, ignoreReplayErrors);
                    break;

                case "EnvironmentKeysModified":
                    WrapDomainEvent<EnvironmentLayerKeysModified>(recordedEvent, ignoreReplayErrors);
                    break;

                case "EnvironmentKeysImported":
                    WrapDomainEvent<EnvironmentLayerKeysImported>(recordedEvent, ignoreReplayErrors);
                    break;

                case "ConfigurationBuilt":
                    WrapDomainEvent<ConfigurationBuilt>(recordedEvent, ignoreReplayErrors);
                    break;

                case "StructureCreated":
                    WrapDomainEvent<StructureCreated>(recordedEvent, ignoreReplayErrors);
                    break;

                case "StructureDeleted":
                    WrapDomainEvent<StructureDeleted>(recordedEvent, ignoreReplayErrors);
                    break;

                case "StructureVariablesModified":
                    WrapDomainEvent<StructureVariablesModified>(recordedEvent, ignoreReplayErrors);
                    break;

                default:
                    throw new MigrationReplayException($"could not handle event of type '{recordedEvent.Event.EventType}'");
            }
        }

        /// <inheritdoc />
        public List<(string Type, byte[] Data, byte[] Metadata)> GenerateEventData()
            => RecordedDomainEvents.Select(e => (e.Type, Data: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(e)), Array.Empty<byte>()))
                                   .ToList();

        private void WrapDomainEvent<T>(ResolvedEvent recordedEvent, bool ignoreReplayErrors)
            where T : DomainEvent
        {
            _options ??= GetEventStoreOptionsAsync().RunSync();

            var maxAppendSize = long.Parse(
                _options?.FirstOrDefault(
                            o => o.Name.Equals(
                                "MaxAppendSize",
                                StringComparison.OrdinalIgnoreCase))
                        ?.Value
                ?? "0");

            try
            {
                var rawEvent = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(recordedEvent.Event.Data.Span));

                // split events into smaller events where necessary
                List<DomainEvent> events = Split(rawEvent, maxAppendSize);
                IEnumerable<LateBindingDomainEvent<DomainEvent>> domainEvents = events.Select(e => new LateBindingDomainEvent<DomainEvent>("Anonymous", e));

                RecordedDomainEvents.AddRange(domainEvents);
            }
            catch (JsonException)
            {
                if (!ignoreReplayErrors)
                {
                    throw new MigrationReplayException($"unable to wrap event of type {typeof(T).Name}");
                }
            }
        }

        private List<DomainEvent> Split<TEvent>(TEvent @event, long maxSize)
            where TEvent : DomainEvent
        {
            var events = new List<DomainEvent> {@event};

            // split into smaller parts as long as anything gets split
            int countBeforeSplit;
            do
            {
                countBeforeSplit = events.Count;

                // split events into smaller parts when they would likely exceed the maxmimum size
                events = events.SelectMany(
                                   e => ApproximateSizeOfDomainEvent(e) >= maxSize
                                            ? e.Split()
                                            : new[] {e})
                               .ToList();
            }
            while (events.Count != countBeforeSplit);

            return events;
        }

        private long ApproximateSizeOfDomainEvent<TEvent>(TEvent @event)
            where TEvent : DomainEvent
        {
            var domainEvent = new DomainEvent<TEvent>("Anonymous", @event);
            string json = JsonConvert.SerializeObject(domainEvent, Formatting.Indented);
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            return bytes.Length;
        }

        private async Task<List<OptionEntry>> GetEventStoreOptionsAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            var storeUri = new Uri(EventStoreConnectionString);

            Uri optionsUri = storeUri.Query.Contains("tls=true", StringComparison.OrdinalIgnoreCase)
                                 ? new Uri($"https://{storeUri.Authority}{storeUri.AbsolutePath}info/options")
                                 : new Uri($"http://{storeUri.Authority}{storeUri.AbsolutePath}info/options");

            // yes creating HttpClient is frowned upon, but we don't need it *that* often and can immediately release it
            var httpClient = new HttpClient();
            HttpResponseMessage response;

            try
            {
                response = await httpClient.GetAsync(optionsUri, cancellationToken);

                if (response is null)
                {
                    return null;
                }
            }
            catch (Exception)
            {
                return null;
            }

            string json = await response.Content.ReadAsStringAsync();
            try
            {
                return JsonConvert.DeserializeObject<List<OptionEntry>>(json);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}