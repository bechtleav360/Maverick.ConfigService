using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers
{
    /// <summary>
    ///     retrieve the history of various objects
    /// </summary>
    [Route(ApiBaseRoute + "history")]
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
        [HttpGet("blame/environment/{category}/{name}")]
        public async Task<IActionResult> BlameEnvironment([FromRoute] string category,
                                                          [FromRoute] string name)
        {
            var events = (await _eventStore.ReplayEvents()).ToArray();

            var blameData = new Dictionary<string, KeyRevision>();

            foreach (var (recordedEvent, domainEvent) in events)
            {
                // we only care about events that modify environment-keys
                if (!(domainEvent is EnvironmentKeysModified keysModified))
                    continue;

                if (!keysModified.Identifier.Category.Equals(category, StringComparison.InvariantCultureIgnoreCase) ||
                    !keysModified.Identifier.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    continue;

                foreach (var action in keysModified.ModifiedKeys)
                {
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
                }
            }

            return Ok(blameData);
        }

        /// <summary>
        ///     get the complete history and metadata of an environment
        /// </summary>
        /// <param name="category"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        [HttpGet("environment/{category}/{name}")]
        public async Task<IActionResult> GetEnvironmentHistory([FromRoute] string category,
                                                               [FromRoute] string name)
        {
            var events = (await _eventStore.ReplayEvents()).ToArray();

            var history = new Dictionary<string, KeyHistory>();

            foreach (var (recordedEvent, domainEvent) in events)
            {
                // we only care about events that modify environment-keys
                if (!(domainEvent is EnvironmentKeysModified keysModified))
                    continue;

                if (!keysModified.Identifier.Category.Equals(category, StringComparison.InvariantCultureIgnoreCase) ||
                    !keysModified.Identifier.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    continue;

                foreach (var action in keysModified.ModifiedKeys)
                {
                    if (!history.ContainsKey(action.Key))
                        history[action.Key] = new KeyHistory
                        {
                            Key = action.Key,
                            History = new SortedList<DateTime, KeyRevision>()
                        };

                    switch (action.Type)
                    {
                        case ConfigKeyActionType.Set:
                            history[action.Key].History.Add(recordedEvent.Created, new KeyRevision
                            {
                                Type = ConfigKeyActionType.Set,
                                DateTime = recordedEvent.Created,
                                Value = action.Value
                            });
                            break;

                        case ConfigKeyActionType.Delete:
                            history[action.Key].History.Add(recordedEvent.Created, new KeyRevision
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
            }

            return Ok(history.Values.OrderBy(r => r.Key));
        }

        // ReSharper disable UnusedAutoPropertyAccessor.Local
        private class KeyRevision
        {
            public DateTime DateTime { get; set; }

            public string Value { get; set; }

            public ConfigKeyActionType Type { get; set; }
        }

        private class KeyHistory
        {
            public string Key { get; set; }

            public SortedList<DateTime, KeyRevision> History { get; set; }
        }
        // ReSharper restore UnusedAutoPropertyAccessor.Local
    }
}