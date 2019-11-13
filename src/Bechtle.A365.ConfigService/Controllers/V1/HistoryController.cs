using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers.V1
{
    /// <summary>
    ///     retrieve the history of various objects
    /// </summary>
    [Route(ApiBaseRoute + "history")]
    [ApiVersion(ApiVersions.V1, Deprecated = ApiDeprecation.V1)]
    public class HistoryController : ControllerBase
    {
        private readonly IEventStore _eventStore;

        /// <inheritdoc />
        public HistoryController(IServiceProvider provider,
                                 ILogger<HistoryController> logger,
                                 IEventStore eventStore)
            : base(provider, logger)
        {
            _eventStore = eventStore;
        }

        /// <summary>
        ///     get all keys within the environment and metadata of their last change
        /// </summary>
        /// <param name="category"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        [HttpGet("blame/environment/{category}/{name}", Name = "Blame")]
        public async Task<IActionResult> BlameEnvironment([FromRoute] string category,
                                                          [FromRoute] string name)
        {
            var blameData = new Dictionary<string, KeyRevision>();

            var comparisonEnvId = new EnvironmentIdentifier(category, name).ToString();

            await _eventStore.ReplayEventsAsStream(
                t => t.RecordedEvent.EventType.Equals(nameof(EnvironmentKeysModified))
                     && t.Metadata[KnownDomainEventMetadata.Identifier].Equals(comparisonEnvId,
                                                                               StringComparison.OrdinalIgnoreCase),
                tuple =>
                {
                    var (recordedEvent, domainEvent) = tuple;

                    if (!(domainEvent is EnvironmentKeysModified keysModified))
                        return true;

                    foreach (var action in keysModified.ModifiedKeys)
                        switch (action.Type)
                        {
                            case ConfigKeyActionType.Set:
                                blameData[action.Key] = new KeyRevision
                                {
                                    DateTime = recordedEvent.Created,
                                    Value = action.Value
                                };
                                break;

                            case ConfigKeyActionType.Delete:
                                if (blameData.ContainsKey(action.Key))
                                    blameData.Remove(action.Key);
                                break;

                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                    return true;
                });

            return Ok(blameData);
        }

        /// <summary>
        ///     get the complete history and metadata of an environment
        /// </summary>
        /// <param name="category"></param>
        /// <param name="name"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        [HttpGet("environment/{category}/{name}", Name = "GetEnvironmentHistory")]
        public async Task<IActionResult> GetEnvironmentHistory([FromRoute] string category,
                                                               [FromRoute] string name,
                                                               [FromQuery] string key = null)
        {
            // key must be url-encoded to be transmitted correctly
            if (!(key is null))
                key = Uri.UnescapeDataString(key);

            var comparisonEnvId = new EnvironmentIdentifier(category, name).ToString();
            var history = new Dictionary<string, KeyHistory>();

            await _eventStore.ReplayEventsAsStream(
                t => t.RecordedEvent.EventType.Equals(nameof(EnvironmentKeysModified))
                     && t.Metadata[KnownDomainEventMetadata.Identifier].Equals(comparisonEnvId,
                                                                               StringComparison.OrdinalIgnoreCase),
                tuple =>
                {
                    var (recordedEvent, domainEvent) = tuple;

                    if (!(domainEvent is EnvironmentKeysModified keysModified))
                        return true;

                    foreach (var action in keysModified.ModifiedKeys)
                    {
                        // if key is set - ignore all keys that don't match with what we're given
                        if (!(key is null) &&
                            !action.Key.StartsWith(key, StringComparison.InvariantCultureIgnoreCase))
                            continue;

                        if (!history.ContainsKey(action.Key))
                            history[action.Key] = new KeyHistory(action.Key);

                        switch (action.Type)
                        {
                            case ConfigKeyActionType.Set:
                                history[action.Key].Changes.Add(recordedEvent.Created, new KeyRevision
                                {
                                    Type = ConfigKeyActionType.Set,
                                    DateTime = recordedEvent.Created,
                                    Value = action.Value
                                });
                                break;

                            case ConfigKeyActionType.Delete:
                                history[action.Key].Changes.Add(recordedEvent.Created, new KeyRevision
                                {
                                    Type = ConfigKeyActionType.Delete,
                                    DateTime = recordedEvent.Created,
                                    Value = action.Value
                                });
                                break;

                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }

                    return true;
                });

            return Ok(history.Values.OrderBy(r => r.Key));
        }

        // ReSharper disable UnusedAutoPropertyAccessor.Local
        private class KeyRevision
        {
            public DateTime DateTime { get; set; }

            public ConfigKeyActionType Type { get; set; }

            public string Value { get; set; }
        }

        private class KeyHistory
        {
            /// <inheritdoc />
            public KeyHistory()
            {
                Key = string.Empty;
                Changes = new SortedList<DateTime, KeyRevision>();
            }

            /// <inheritdoc />
            public KeyHistory(string key) : this()
            {
                Key = key;
            }

            // ReSharper disable once CollectionNeverQueried.Local
            public SortedList<DateTime, KeyRevision> Changes { get; }

            public string Key { get; }
        }
        // ReSharper restore UnusedAutoPropertyAccessor.Local
    }
}