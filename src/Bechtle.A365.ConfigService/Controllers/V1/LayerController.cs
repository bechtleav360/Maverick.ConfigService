using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Objects;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Implementations;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers.V1
{
    /// <summary>
    ///     Query / Manage Layer-Data
    /// </summary>
    [Route(ApiBaseRoute + "layers")]
    [ApiVersion(ApiVersions.V1, Deprecated = ApiDeprecation.V1)]
    public class LayerController : ControllerBase
    {
        private readonly IProjectionStore _store;
        private readonly IJsonTranslator _translator;

        /// <inheritdoc />
        public LayerController(IServiceProvider provider,
                               ILogger<LayerController> logger,
                               IProjectionStore store,
                               IJsonTranslator translator)
            : base(provider, logger)
        {
            _store = store;
            _translator = translator;
        }

        /// <summary>
        ///     create a new Layer with the given Name
        /// </summary>
        /// <param name="name">Name of the given Layer</param>
        /// <returns>redirects to 'GetKeys'-operation</returns>
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.Accepted)]
        [HttpPost("{name}", Name = "AddLayer")]
        public async Task<IActionResult> AddLayer(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest($"{nameof(name)} is empty");

            try
            {
                var result = await _store.Layers.Create(new LayerIdentifier(name));
                if (result.IsError)
                    return ProviderError(result);

                return AcceptedAtAction(nameof(GetKeys),
                                        RouteUtilities.ControllerName<LayerController>(),
                                        new {version = ApiVersions.V1, name});
            }
            catch (Exception e)
            {
                KnownMetrics.Exception.WithLabels(e.GetType().Name).Inc();
                Logger.LogError(e, $"failed to add new layer ({nameof(name)}: {name})");
                return StatusCode(HttpStatusCode.InternalServerError, "failed to add new layer");
            }
        }

        /// <summary>
        ///     delete an existing Layer with the given Name
        /// </summary>
        /// <param name="name">Name of the given Layer</param>
        /// <returns></returns>
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.Accepted)]
        [HttpDelete("{name}", Name = "DeleteLayer")]
        public async Task<IActionResult> DeleteLayer(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest($"{nameof(name)} is empty");

            try
            {
                var result = await _store.Layers.Delete(new LayerIdentifier(name));
                if (result.IsError)
                    return ProviderError(result);

                return Accepted();
            }
            catch (Exception e)
            {
                KnownMetrics.Exception.WithLabels(e.GetType().Name).Inc();
                Logger.LogError(e, $"failed to delete layer ({nameof(name)}: {name})");
                return StatusCode(HttpStatusCode.InternalServerError, "failed to delete layer");
            }
        }

        /// <summary>
        ///     get a list of available layers
        /// </summary>
        /// <param name="offset">offset from the beginning of the returned query-results</param>
        /// <param name="length">amount of items to return in the given "page"</param>
        /// <param name="targetVersion">Event-Version to use for this operation</param>
        /// <returns>list of Layer-Ids</returns>
        [ProducesResponseType(typeof(LayerIdentifier[]), (int) HttpStatusCode.OK)]
        [HttpGet(Name = "GetLayers")]
        public async Task<IActionResult> GetAvailableLayers([FromQuery] int offset = -1,
                                                            [FromQuery] int length = -1,
                                                            [FromQuery] long targetVersion = -1)
        {
            var range = QueryRange.Make(offset, length);

            try
            {
                var result = await _store.Layers.GetAvailable(range, targetVersion);

                return Result(result);
            }
            catch (Exception e)
            {
                KnownMetrics.Exception.WithLabels(e.GetType().Name).Inc();
                Logger.LogError(e, "failed to retrieve available Layers (" +
                                   $"{nameof(offset)}: {offset}; " +
                                   $"{nameof(length)}: {length}; " +
                                   $"{nameof(targetVersion)}: {targetVersion})");
                return StatusCode(HttpStatusCode.InternalServerError, "failed to retrieve available layers");
            }
        }

        /// <summary>
        ///     get the keys contained in an environment
        /// </summary>
        /// <param name="name">Name of the given Layer</param>
        /// <param name="filter">Key-Path based filter to apply to all results. filters out items not matching this path</param>
        /// <param name="preferExactMatch">same as 'Filter', but will only return exact matches (useful for filtering sub-keys that share parts of their names)</param>
        /// <param name="root">root to assume when returning items. Will be removed from all keys, if all returned keys start with the given 'Root'</param>
        /// <param name="offset">offset from the beginning of the returned query-results</param>
        /// <param name="length">amount of items to return in the given "page"</param>
        /// <param name="targetVersion">Event-Version to use for this operation</param>
        /// <returns>Key-Value map</returns>
        [ProducesResponseType(typeof(Dictionary<string, string>), (int) HttpStatusCode.OK)]
        [HttpGet("{name}/keys", Name = "GetLayerAsKeys")]
        public async Task<IActionResult> GetKeys([FromRoute] string name,
                                                 [FromQuery] string filter,
                                                 [FromQuery] string preferExactMatch,
                                                 [FromQuery] string root,
                                                 [FromQuery] int offset = -1,
                                                 [FromQuery] int length = -1,
                                                 [FromQuery] long targetVersion = -1)
        {
            var range = QueryRange.Make(offset, length);

            var identifier = new LayerIdentifier(name);
            try
            {
                var result = await _store.Layers.GetKeys(new KeyQueryParameters<LayerIdentifier>
                {
                    Identifier = identifier,
                    Filter = filter,
                    PreferExactMatch = preferExactMatch,
                    Range = range,
                    RemoveRoot = root,
                    TargetVersion = targetVersion
                });

                return result.IsError
                           ? ProviderError(result)
                           : Ok(result.Data.ToImmutableSortedDictionary());
            }
            catch (Exception e)
            {
                KnownMetrics.Exception.WithLabels(e.GetType().Name).Inc();
                Logger.LogError(e, "failed to retrieve Layer-Keys (" +
                                   $"{nameof(name)}: {name}; " +
                                   $"{nameof(filter)}: {filter}; " +
                                   $"{nameof(preferExactMatch)}: {preferExactMatch}; " +
                                   $"{nameof(root)}: {root}; " +
                                   $"{nameof(offset)}: {offset}; " +
                                   $"{nameof(length)}: {length}; " +
                                   $"{nameof(targetVersion)}: {targetVersion})");
                return StatusCode(HttpStatusCode.InternalServerError, "failed to retrieve layer-keys");
            }
        }

        /// <summary>
        ///     get the keys contained in an environment, converted to JSON
        /// </summary>
        /// <param name="name">Name of the given Layer</param>
        /// <param name="filter">Key-Path based filter to apply to all results. filters out items not matching this path</param>
        /// <param name="preferExactMatch">same as 'Filter', but will only return exact matches (useful for filtering sub-keys that share parts of their names)</param>
        /// <param name="root">root to assume when returning items. Will be removed from all keys, if all returned keys start with the given 'Root'</param>
        /// <param name="targetVersion">Event-Version to use for this operation</param>
        /// <returns>environment-keys formatted as JSON</returns>
        [ProducesResponseType(typeof(object), (int) HttpStatusCode.OK)]
        [HttpGet("{name}/json", Name = "GetLayerAsJson")]
        public async Task<IActionResult> GetKeysAsJson([FromRoute] string name,
                                                       [FromQuery] string filter,
                                                       [FromQuery] string preferExactMatch,
                                                       [FromQuery] string root,
                                                       [FromQuery] long targetVersion = -1)
        {
            try
            {
                var identifier = new LayerIdentifier(name);

                var result = await _store.Layers.GetKeys(new KeyQueryParameters<LayerIdentifier>
                {
                    Identifier = identifier,
                    Filter = filter,
                    PreferExactMatch = preferExactMatch,
                    Range = QueryRange.All,
                    RemoveRoot = root,
                    TargetVersion = targetVersion
                });

                if (result.IsError)
                    return ProviderError(result);

                var json = _translator.ToJson(result.Data);

                return Ok(json);
            }
            catch (Exception e)
            {
                KnownMetrics.Exception.WithLabels(e.GetType().Name).Inc();
                Logger.LogError(e, "failed to retrieve Layer-Keys (" +
                                   $"{nameof(name)}: {name}; " +
                                   $"{nameof(filter)}: {filter}; " +
                                   $"{nameof(preferExactMatch)}: {preferExactMatch}; " +
                                   $"{nameof(root)}: {root}; " +
                                   $"{nameof(targetVersion)}: {targetVersion})");
                return StatusCode(HttpStatusCode.InternalServerError, "failed to retrieve layer as json");
            }
        }

        /// <summary>
        ///     get the keys contained in an environment including all their metadata
        /// </summary>
        /// <param name="name">Name of the given Layer</param>
        /// <param name="filter">Key-Path based filter to apply to all results. filters out items not matching this path</param>
        /// <param name="preferExactMatch">same as 'Filter', but will only return exact matches (useful for filtering sub-keys that share parts of their names)</param>
        /// <param name="root">root to assume when returning items. Will be removed from all keys, if all returned keys start with the given 'Root'</param>
        /// <param name="offset">offset from the beginning of the returned query-results</param>
        /// <param name="length">amount of items to return in the given "page"</param>
        /// <param name="targetVersion">Event-Version to use for this operation</param>
        /// <returns>Key/Value-Objects</returns>
        [ProducesResponseType(typeof(EnvironmentLayerKey), (int) HttpStatusCode.OK)]
        [HttpGet("{name}/objects", Name = "GetLayerAsObjects")]
        public async Task<IActionResult> GetKeysWithMetadata([FromRoute] string name,
                                                             [FromQuery] string filter,
                                                             [FromQuery] string preferExactMatch,
                                                             [FromQuery] string root,
                                                             [FromQuery] int offset = -1,
                                                             [FromQuery] int length = -1,
                                                             [FromQuery] long targetVersion = -1)
        {
            try
            {
                var range = QueryRange.Make(offset, length);

                var identifier = new LayerIdentifier(name);

                var result = await _store.Layers.GetKeyObjects(new KeyQueryParameters<LayerIdentifier>
                {
                    Identifier = identifier,
                    Filter = filter,
                    PreferExactMatch = preferExactMatch,
                    Range = range,
                    RemoveRoot = root,
                    TargetVersion = targetVersion
                });

                if (result.IsError)
                    return ProviderError(result);

                foreach (var item in result.Data)
                {
                    if (item.Description is null)
                        item.Description = string.Empty;
                    if (item.Type is null)
                        item.Type = string.Empty;
                }

                return Ok(result.Data);
            }
            catch (Exception e)
            {
                KnownMetrics.Exception.WithLabels(e.GetType().Name).Inc();
                Logger.LogError(e, "failed to retrieve Layer-Keys (" +
                                   $"{nameof(name)}: {name}; " +
                                   $"{nameof(filter)}: {filter}; " +
                                   $"{nameof(preferExactMatch)}: {preferExactMatch}; " +
                                   $"{nameof(root)}: {root}; " +
                                   $"{nameof(offset)}: {offset}; " +
                                   $"{nameof(length)}: {length}; " +
                                   $"{nameof(targetVersion)}: {targetVersion})");
                return StatusCode(HttpStatusCode.InternalServerError, "failed to retrieve layer-keys");
            }
        }

        /// <summary>
        ///     delete keys from the environment
        /// </summary>
        /// <param name="name">Name of the given Layer</param>
        /// <param name="keys">list of keys to remove from the Layer</param>
        /// <returns></returns>
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.Accepted)]
        [HttpDelete("{name}/keys", Name = "DeleteFromLayer")]
        public async Task<IActionResult> DeleteKeys([FromRoute] string name,
                                                    [FromBody] string[] keys)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest($"{nameof(name)} is empty");

            if (keys == null || !keys.Any())
                return BadRequest("no keys received");

            try
            {
                var result = await _store.Layers.DeleteKeys(new LayerIdentifier(name), keys);
                if (result.IsError)
                    return ProviderError(result);

                return AcceptedAtAction(nameof(GetKeys),
                                        RouteUtilities.ControllerName<LayerController>(),
                                        new {version = ApiVersions.V1, name});
            }
            catch (Exception e)
            {
                KnownMetrics.Exception.WithLabels(e.GetType().Name).Inc();
                Logger.LogError(e, $"failed to delete keys from Layer ({nameof(name)}: {name})");
                return StatusCode(HttpStatusCode.InternalServerError, "failed to delete keys");
            }
        }

        /// <summary>
        ///     add or update keys in the environment
        /// </summary>
        /// <param name="name">Name of the given Layer</param>
        /// <param name="keys">Keys to Set / Update</param>
        /// <returns>redirects to 'GetKeys' action</returns>
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.Accepted)]
        [HttpPut("{name}/keys", Name = "UpdateLayer")]
        public async Task<IActionResult> UpdateKeys([FromRoute] string name,
                                                    [FromBody] DtoConfigKey[] keys)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest($"{nameof(name)} is empty");

            if (keys == null || !keys.Any())
                return BadRequest("no keys received");

            var groups = keys.GroupBy(k => k.Key)
                             .ToArray();

            if (groups.Any(g => g.Count() > 1))
                return BadRequest("duplicate keys received: " +
                                  string.Join(';',
                                              groups.Where(g => g.Count() > 1)
                                                    .Select(g => $"'{g.Key}'")));

            try
            {
                var result = await _store.Layers.UpdateKeys(new LayerIdentifier(name), keys);
                if (result.IsError)
                    return ProviderError(result);

                return AcceptedAtAction(nameof(GetKeys),
                                        RouteUtilities.ControllerName<LayerController>(),
                                        new {version = ApiVersions.V1, name});
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"failed to update keys of Layer ({nameof(name)}: {name})");
                return StatusCode(HttpStatusCode.InternalServerError, "failed to update keys");
            }
        }
    }
}