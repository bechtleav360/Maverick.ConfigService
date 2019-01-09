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
        /// <param name="when"></param>
        /// <returns></returns>
        [HttpGet("blame/environment/{category}/{name}")]
        public async Task<IActionResult> BlameEnvironment([FromRoute] string category,
                                                          [FromRoute] string name,
                                                          [FromQuery] DateTime when)
        {
            var events = (await _eventStore.ReplayEvents()).ToArray();

            var blameData = new Dictionary<string, KeyBlameData>();

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
                            blameData[action.Key] = new KeyBlameData
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

        // TODO: find a better name?
        private class KeyBlameData
        {
            public DateTime DateTime { get; set; }

            public string Value { get; set; }
        }
    }
}