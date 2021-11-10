using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Implementations;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers.V1
{
    /// <summary>
    ///     Query / Manage Environment-Data
    /// </summary>
    [Route(ApiBaseRoute + "environments")]
    public class EnvironmentController : ControllerBase
    {
        private readonly IProjectionStore _store;
        private readonly IJsonTranslator _translator;

        /// <inheritdoc />
        public EnvironmentController(
            ILogger<EnvironmentController> logger,
            IProjectionStore store,
            IJsonTranslator translator)
            : base(logger)
        {
            _store = store;
            _translator = translator;
        }

        /// <summary>
        ///     create a new Environment with the given Category + Name
        /// </summary>
        /// <param name="category">Category of the requested Environment</param>
        /// <param name="name">Name of the given Environment</param>
        /// <returns>redirects to 'GetKeys'-operation</returns>
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.Accepted)]
        [HttpPost("{category}/{name}", Name = "AddEnvironment")]
        [ApiVersion(ApiVersions.V1, Deprecated = ApiDeprecation.V1)]
        [ApiVersion(ApiVersions.V11, Deprecated = ApiDeprecation.V11)]
        public async Task<IActionResult> AddEnvironment(string category, string name)
        {
            if (string.IsNullOrWhiteSpace(category))
                return BadRequest($"{nameof(category)} is empty");

            if (string.IsNullOrWhiteSpace(name))
                return BadRequest($"{nameof(name)} is empty");

            try
            {
                var result = await _store.Environments.Create(new EnvironmentIdentifier(category, name), false);
                if (result.IsError)
                    return ProviderError(result);

                return AcceptedAtAction(
                    nameof(GetKeys),
                    RouteUtilities.ControllerName<EnvironmentController>(),
                    new { version = ApiVersions.V1, category, name });
            }
            catch (Exception e)
            {
                KnownMetrics.Exception.WithLabels(e.GetType().Name).Inc();
                Logger.LogError(
                    e,
                    "failed to add new environment at (Category='{Category}'; Name='{Name}')",
                    category,
                    name);
                return StatusCode(HttpStatusCode.InternalServerError, "failed to add new environment");
            }
        }

        /// <summary>
        ///     assign the given layers in their given order as the content of this Environment
        /// </summary>
        /// <param name="category">Category of the requested Environment</param>
        /// <param name="name">Name of the given Environment</param>
        /// <param name="layers">ordered list of Layers to be assigned</param>
        /// <returns></returns>
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.Accepted)]
        [HttpPut("{category}/{name}/layers", Name = "AssignLayers")]
        [ApiVersion(ApiVersions.V1, Deprecated = ApiDeprecation.V1)]
        [ApiVersion(ApiVersions.V11, Deprecated = ApiDeprecation.V11)]
        public async Task<IActionResult> AssignLayers(
            [FromRoute] string category,
            [FromRoute] string name,
            [FromBody] LayerIdentifier[] layers)
        {
            try
            {
                var result = await _store.Environments.AssignLayers(
                                 new EnvironmentIdentifier(category, name),
                                 layers);

                if (result.IsError)
                    return ProviderError(result);

                return Accepted();
            }
            catch (Exception e)
            {
                KnownMetrics.Exception.WithLabels(e.GetType().Name).Inc();
                Logger.LogError(
                    e,
                    "failed to assign Layers to Environment (Category='{category}'; Name='{Name}')",
                    category,
                    name);
                return StatusCode(HttpStatusCode.InternalServerError, "failed to assign layers to environment");
            }
        }

        /// <summary>
        ///     delete an existing Environment with the given Category + Name
        /// </summary>
        /// <param name="category">Category of the requested Environment</param>
        /// <param name="name">Name of the given Environment</param>
        /// <returns></returns>
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.Accepted)]
        [HttpDelete("{category}/{name}", Name = "DeleteEnvironment")]
        [ApiVersion(ApiVersions.V1, Deprecated = ApiDeprecation.V1)]
        [ApiVersion(ApiVersions.V11, Deprecated = ApiDeprecation.V11)]
        public async Task<IActionResult> DeleteEnvironment(string category, string name)
        {
            if (string.IsNullOrWhiteSpace(category))
                return BadRequest($"{nameof(category)} is empty");

            if (string.IsNullOrWhiteSpace(name))
                return BadRequest($"{nameof(name)} is empty");

            try
            {
                var result = await _store.Environments.Delete(new EnvironmentIdentifier(category, name));
                if (result.IsError)
                    return ProviderError(result);

                return Accepted();
            }
            catch (Exception e)
            {
                KnownMetrics.Exception.WithLabels(e.GetType().Name).Inc();
                Logger.LogError(
                    e,
                    "failed to delete environment at (Category='{Category}'; Name='{Name}')",
                    category,
                    name);
                return StatusCode(HttpStatusCode.InternalServerError, "failed to delete environment");
            }
        }

        /// <summary>
        ///     get the assigned layers and their order for this Environment
        /// </summary>
        /// <param name="category">Category of the requested Environment</param>
        /// <param name="name">Name of the given Environment</param>
        /// <returns>ordered list of assigned layer-ids</returns>
        [ProducesResponseType(typeof(LayerIdentifier[]), (int)HttpStatusCode.OK)]
        [ApiVersion(ApiVersions.V1, Deprecated = ApiDeprecation.V1)]
        [ApiVersion(ApiVersions.V11, Deprecated = ApiDeprecation.V11)]
        [HttpGet("{category}/{name}/layers", Name = "GetAssignedLayers")]
        public async Task<IActionResult> GetAssignedLayers(
            [FromRoute] string category,
            [FromRoute] string name)
        {
            try
            {
                var result = await _store.Environments.GetAssignedLayers(new EnvironmentIdentifier(category, name));

                if (result.IsError)
                    return ProviderError(result);
                return Ok(result.CheckedData.Items);
            }
            catch (Exception e)
            {
                KnownMetrics.Exception.WithLabels(e.GetType().Name).Inc();
                Logger.LogError(
                    e,
                    "failed to retrieve assigned Layers for Environment (Category='{Category}'; Name='{Name}')",
                    category,
                    name);
                return StatusCode(HttpStatusCode.InternalServerError, "failed to retrieve assigned layers for environment");
            }
        }

        /// <summary>
        ///     get a list of available environments
        /// </summary>
        /// <param name="offset">offset from the beginning of the returned query-results</param>
        /// <param name="length">amount of items to return in the given "page"</param>
        /// <param name="targetVersion">Event-Version to use for this operation</param>
        /// <returns>list of Environment-Ids</returns>
        [ProducesResponseType(typeof(EnvironmentIdentifier[]), (int)HttpStatusCode.OK)]
        [HttpGet("available", Name = "GetAvailableEnvironments")]
        [Obsolete("use GetEnvironments (GET /) instead")]
        [ApiVersion(ApiVersions.V1, Deprecated = ApiDeprecation.V1)]
        public IActionResult GetAvailableEnvironments(
            [FromQuery] int offset = -1,
            [FromQuery] int length = -1,
            [FromQuery] long targetVersion = -1)
            => RedirectToActionPermanent(
                nameof(GetEnvironments),
                RouteUtilities.ControllerName<EnvironmentController>(),
                new { offset, length, targetVersion, version = ApiVersions.V1 });

        /// <summary>
        ///     get a list of available environments
        /// </summary>
        /// <param name="offset">offset from the beginning of the returned query-results</param>
        /// <param name="length">amount of items to return in the given "page"</param>
        /// <param name="targetVersion">Event-Version to use for this operation</param>
        /// <returns>list of Environment-Ids</returns>
        [ProducesResponseType(typeof(EnvironmentIdentifier[]), (int)HttpStatusCode.OK)]
        [HttpGet(Name = "GetEnvironments")]
        [ApiVersion(ApiVersions.V1, Deprecated = ApiDeprecation.V1)]
        public async Task<IActionResult> GetEnvironments(
            [FromQuery] int offset = -1,
            [FromQuery] int length = -1,
            [FromQuery] long targetVersion = -1)
        {
            var range = QueryRange.Make(offset, length);

            try
            {
                var result = await _store.Environments.GetAvailable(range, targetVersion);

                if (result.IsError)
                    return ProviderError(result);
                return Ok(result.CheckedData.Items);
            }
            catch (Exception e)
            {
                KnownMetrics.Exception.WithLabels(e.GetType().Name).Inc();
                Logger.LogError(
                    e,
                    "failed to retrieve available Environments "
                    + "(Offset='{Offset}'; Length='{Length}'; TargetVersion='{TargetVersion}')",
                    offset,
                    length,
                    targetVersion);
                return StatusCode(HttpStatusCode.InternalServerError, "failed to retrieve available environments");
            }
        }

        /// <summary>
        ///     get a list of available environments
        /// </summary>
        /// <param name="offset">offset from the beginning of the returned query-results</param>
        /// <param name="length">amount of items to return in the given "page"</param>
        /// <param name="targetVersion">Event-Version to use for this operation</param>
        /// <returns>list of Environment-Ids</returns>
        [ProducesResponseType(typeof(EnvironmentIdentifier[]), (int)HttpStatusCode.OK)]
        [HttpGet(Name = "GetEnvironmentsPaged")]
        [ApiVersion(ApiVersions.V11, Deprecated = ApiDeprecation.V11)]
        public async Task<IActionResult> GetEnvironmentsPaged(
            [FromQuery] int offset = -1,
            [FromQuery] int length = -1,
            [FromQuery] long targetVersion = -1)
        {
            var range = QueryRange.Make(offset, length);

            try
            {
                return Result(await _store.Environments.GetMetadata(range, targetVersion));
            }
            catch (Exception e)
            {
                KnownMetrics.Exception.WithLabels(e.GetType().Name).Inc();
                Logger.LogError(
                    e,
                    "failed to retrieve available Environments "
                    + "(Offset='{Offset}'; Length='{Length}'; TargetVersion='{TargetVersion}')",
                    offset,
                    length,
                    targetVersion);
                return StatusCode(HttpStatusCode.InternalServerError, "failed to retrieve available environments");
            }
        }

        /// <summary>
        ///     get the keys contained in an environment
        /// </summary>
        /// <param name="category">Category of the requested Environment</param>
        /// <param name="name">Name of the given Environment</param>
        /// <param name="filter">Key-Path based filter to apply to all results. filters out items not matching this path</param>
        /// <param name="preferExactMatch">same as 'Filter', but will only return exact matches (useful for filtering sub-keys that share parts of their names)</param>
        /// <param name="root">root to assume when returning items. Will be removed from all keys, if all returned keys start with the given 'Root'</param>
        /// <param name="offset">offset from the beginning of the returned query-results</param>
        /// <param name="length">amount of items to return in the given "page"</param>
        /// <param name="targetVersion">Event-Version to use for this operation</param>
        /// <returns>Key-Value map</returns>
        [ProducesResponseType(typeof(Dictionary<string, string>), (int)HttpStatusCode.OK)]
        [HttpGet("{category}/{name}/keys", Name = "GetEnvironmentAsKeys")]
        [ApiVersion(ApiVersions.V1, Deprecated = ApiDeprecation.V1)]
        public async Task<IActionResult> GetKeys(
            [FromRoute] string category,
            [FromRoute] string name,
            [FromQuery] string? filter = null,
            [FromQuery] string? preferExactMatch = null,
            [FromQuery] string? root = null,
            [FromQuery] int offset = -1,
            [FromQuery] int length = -1,
            [FromQuery] long targetVersion = -1)
        {
            var range = QueryRange.Make(offset, length);

            var identifier = new EnvironmentIdentifier(category, name);
            try
            {
                var result = await _store.Environments.GetKeys(
                                 new KeyQueryParameters<EnvironmentIdentifier>
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
                           : Ok(result.CheckedData.ToImmutableSortedDictionary());
            }
            catch (Exception e)
            {
                KnownMetrics.Exception.WithLabels(e.GetType().Name).Inc();
                Logger.LogError(
                    e,
                    "failed to retrieve Environment-Keys ("
                    + "Category='{Category}'; Name='{Name}'; "
                    + "Filter='{Filter}'; PreferExactMatch='{PreferExactMatch}'; Root='{Root}'; "
                    + "Offset='{Offset}'; Length='{Length}'; TargetVersion='{TargetVersion}')",
                    category,
                    name,
                    filter,
                    preferExactMatch,
                    root,
                    offset,
                    length,
                    targetVersion);
                return StatusCode(HttpStatusCode.InternalServerError, "failed to retrieve environment-keys");
            }
        }

        /// <summary>
        ///     get the keys contained in an environment
        /// </summary>
        /// <param name="category">Category of the requested Environment</param>
        /// <param name="name">Name of the given Environment</param>
        /// <param name="filter">Key-Path based filter to apply to all results. filters out items not matching this path</param>
        /// <param name="preferExactMatch">same as 'Filter', but will only return exact matches (useful for filtering sub-keys that share parts of their names)</param>
        /// <param name="root">root to assume when returning items. Will be removed from all keys, if all returned keys start with the given 'Root'</param>
        /// <param name="offset">offset from the beginning of the returned query-results</param>
        /// <param name="length">amount of items to return in the given "page"</param>
        /// <param name="targetVersion">Event-Version to use for this operation</param>
        /// <returns>Key-Value map</returns>
        [ProducesResponseType(typeof(Dictionary<string, string>), (int)HttpStatusCode.OK)]
        [HttpGet("{category}/{name}/keys", Name = "GetEnvironmentAsKeysPaged")]
        [ApiVersion(ApiVersions.V11, Deprecated = ApiDeprecation.V11)]
        public async Task<IActionResult> GetKeysPaged(
            [FromRoute] string category,
            [FromRoute] string name,
            [FromQuery] string? filter = null,
            [FromQuery] string? preferExactMatch = null,
            [FromQuery] string? root = null,
            [FromQuery] int offset = -1,
            [FromQuery] int length = -1,
            [FromQuery] long targetVersion = -1)
        {
            var range = QueryRange.Make(offset, length);

            var identifier = new EnvironmentIdentifier(category, name);
            try
            {
                return Result(
                    await _store.Environments.GetKeys(
                        new KeyQueryParameters<EnvironmentIdentifier>
                        {
                            Identifier = identifier,
                            Filter = filter,
                            PreferExactMatch = preferExactMatch,
                            Range = range,
                            RemoveRoot = root,
                            TargetVersion = targetVersion
                        }));
            }
            catch (Exception e)
            {
                KnownMetrics.Exception.WithLabels(e.GetType().Name).Inc();
                Logger.LogError(
                    e,
                    "failed to retrieve Environment-Keys ("
                    + "Category='{Category}'; Name='{Name}'; "
                    + "Filter='{Filter}'; PreferExactMatch='{PreferExactMatch}'; Root='{Root}'; "
                    + "Offset='{Offset}'; Length='{Length}'; TargetVersion='{TargetVersion}')",
                    category,
                    name,
                    filter,
                    preferExactMatch,
                    root,
                    offset,
                    length,
                    targetVersion);
                return StatusCode(HttpStatusCode.InternalServerError, "failed to retrieve environment-keys");
            }
        }

        /// <summary>
        ///     get the keys contained in an environment, converted to JSON
        /// </summary>
        /// <param name="category">Category of the requested Environment</param>
        /// <param name="name">Name of the given Environment</param>
        /// <param name="filter">Key-Path based filter to apply to all results. filters out items not matching this path</param>
        /// <param name="preferExactMatch">same as 'Filter', but will only return exact matches (useful for filtering sub-keys that share parts of their names)</param>
        /// <param name="root">root to assume when returning items. Will be removed from all keys, if all returned keys start with the given 'Root'</param>
        /// <param name="targetVersion">Event-Version to use for this operation</param>
        /// <returns>environment-keys formatted as JSON</returns>
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.OK)]
        [HttpGet("{category}/{name}/json", Name = "GetEnvironmentAsJson")]
        [ApiVersion(ApiVersions.V1, Deprecated = ApiDeprecation.V1)]
        [ApiVersion(ApiVersions.V11, Deprecated = ApiDeprecation.V11)]
        public async Task<IActionResult> GetKeysAsJson(
            [FromRoute] string category,
            [FromRoute] string name,
            [FromQuery] string? filter = null,
            [FromQuery] string? preferExactMatch = null,
            [FromQuery] string? root = null,
            [FromQuery] long targetVersion = -1)
        {
            try
            {
                var identifier = new EnvironmentIdentifier(category, name);

                var result = await _store.Environments.GetKeys(
                                 new KeyQueryParameters<EnvironmentIdentifier>
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

                var json = _translator.ToJson(result.CheckedData.Items);

                return Ok(json);
            }
            catch (Exception e)
            {
                KnownMetrics.Exception.WithLabels(e.GetType().Name).Inc();
                Logger.LogError(
                    e,
                    "failed to retrieve Environment-Json ("
                    + "Category='{Category}'; Name='{Name}'; "
                    + "Filter='{Filter}'; PreferExactMatch='{PreferExactMatch}'; Root='{Root}'; "
                    + "TargetVersion='{TargetVersion}')",
                    category,
                    name,
                    filter,
                    preferExactMatch,
                    root,
                    targetVersion);
                return StatusCode(HttpStatusCode.InternalServerError, "failed to retrieve environment as json");
            }
        }

        /// <summary>
        ///     get the keys contained in an environment including all their metadata
        /// </summary>
        /// <param name="category">Category of the requested Environment</param>
        /// <param name="name">Name of the given Environment</param>
        /// <param name="filter">Key-Path based filter to apply to all results. filters out items not matching this path</param>
        /// <param name="preferExactMatch">same as 'Filter', but will only return exact matches (useful for filtering sub-keys that share parts of their names)</param>
        /// <param name="root">root to assume when returning items. Will be removed from all keys, if all returned keys start with the given 'Root'</param>
        /// <param name="offset">offset from the beginning of the returned query-results</param>
        /// <param name="length">amount of items to return in the given "page"</param>
        /// <param name="targetVersion">Event-Version to use for this operation</param>
        /// <returns>Key/Value-Objects</returns>
        [ProducesResponseType(typeof(EnvironmentLayerKey), (int)HttpStatusCode.OK)]
        [HttpGet("{category}/{name}/keys/objects", Name = "GetEnvironmentAsObjects")]
        [ApiVersion(ApiVersions.V1, Deprecated = ApiDeprecation.V1)]
        public async Task<IActionResult> GetKeysWithMetadata(
            [FromRoute] string category,
            [FromRoute] string name,
            [FromQuery] string? filter = null,
            [FromQuery] string? preferExactMatch = null,
            [FromQuery] string? root = null,
            [FromQuery] int offset = -1,
            [FromQuery] int length = -1,
            [FromQuery] long targetVersion = -1)
        {
            try
            {
                var range = QueryRange.Make(offset, length);

                var identifier = new EnvironmentIdentifier(category, name);

                var result = await _store.Environments.GetKeyObjects(
                                 new KeyQueryParameters<EnvironmentIdentifier>
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

                return Ok(result.CheckedData.Items);
            }
            catch (Exception e)
            {
                KnownMetrics.Exception.WithLabels(e.GetType().Name).Inc();
                Logger.LogError(
                    e,
                    "failed to retrieve Environment-Metadata ("
                    + "Category='{Category}'; Name='{Name}'; "
                    + "Filter='{Filter}'; PreferExactMatch='{PreferExactMatch}'; Root='{Root}'; "
                    + "Offset='{Offset}'; Length='{Length}'; TargetVersion='{TargetVersion}')",
                    category,
                    name,
                    filter,
                    preferExactMatch,
                    root,
                    offset,
                    length,
                    targetVersion);
                return StatusCode(HttpStatusCode.InternalServerError, "failed to retrieve environment-metadata");
            }
        }

        /// <summary>
        ///     get the keys contained in an environment including all their metadata
        /// </summary>
        /// <param name="category">Category of the requested Environment</param>
        /// <param name="name">Name of the given Environment</param>
        /// <param name="filter">Key-Path based filter to apply to all results. filters out items not matching this path</param>
        /// <param name="preferExactMatch">same as 'Filter', but will only return exact matches (useful for filtering sub-keys that share parts of their names)</param>
        /// <param name="root">root to assume when returning items. Will be removed from all keys, if all returned keys start with the given 'Root'</param>
        /// <param name="offset">offset from the beginning of the returned query-results</param>
        /// <param name="length">amount of items to return in the given "page"</param>
        /// <param name="targetVersion">Event-Version to use for this operation</param>
        /// <returns>Key/Value-Objects</returns>
        [ProducesResponseType(typeof(EnvironmentLayerKey), (int)HttpStatusCode.OK)]
        [HttpGet("{category}/{name}/keys/objects", Name = "GetEnvironmentAsObjectsPaged")]
        [ApiVersion(ApiVersions.V11, Deprecated = ApiDeprecation.V11)]
        public async Task<IActionResult> GetKeysWithMetadataPaged(
            [FromRoute] string category,
            [FromRoute] string name,
            [FromQuery] string? filter = null,
            [FromQuery] string? preferExactMatch = null,
            [FromQuery] string? root = null,
            [FromQuery] int offset = -1,
            [FromQuery] int length = -1,
            [FromQuery] long targetVersion = -1)
        {
            try
            {
                var range = QueryRange.Make(offset, length);

                var identifier = new EnvironmentIdentifier(category, name);

                var result = await _store.Environments.GetKeyObjects(
                                 new KeyQueryParameters<EnvironmentIdentifier>
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

                return Ok(result.Data);
            }
            catch (Exception e)
            {
                KnownMetrics.Exception.WithLabels(e.GetType().Name).Inc();
                Logger.LogError(
                    e,
                    "failed to retrieve Environment-Key-Objects ("
                    + "Category='{Category}'; Name='{Name}'; "
                    + "Filter='{Filter}'; PreferExactMatch='{PreferExactMatch}'; Root='{Root}'; "
                    + "Offset='{Offset}'; Length='{Length}'; TargetVersion='{TargetVersion}')",
                    category,
                    name,
                    filter,
                    preferExactMatch,
                    root,
                    offset,
                    length,
                    targetVersion);
                return StatusCode(HttpStatusCode.InternalServerError, "failed to retrieve environment-key-objects");
            }
        }

        /// <summary>
        ///     get all metadata for an Environment
        /// </summary>
        /// <param name="category">Category of the requested Environment</param>
        /// <param name="name">Name of the given Environment</param>
        /// <returns>Metadata for the environment</returns>
        [ProducesResponseType(typeof(ConfigEnvironmentMetadata), (int)HttpStatusCode.OK)]
        [HttpGet("{category}/{name}/info", Name = "GetEnvironmentMetadata")]
        [ApiVersion(ApiVersions.V11, Deprecated = ApiDeprecation.V11)]
        public async Task<IActionResult> GetMetadata(
            [FromRoute] string category,
            [FromRoute] string name)
        {
            var identifier = new EnvironmentIdentifier(category, name);
            try
            {
                IResult<ConfigEnvironmentMetadata> result = await _store.Environments.GetMetadata(identifier);

                return Result(result);
            }
            catch (Exception e)
            {
                KnownMetrics.Exception.WithLabels(e.GetType().Name).Inc();
                Logger.LogError(
                    e,
                    "failed to retrieve Environment-Metadata (Category='{Category}'; Name='{Name}')",
                    category,
                    name);
                return StatusCode(HttpStatusCode.InternalServerError, "failed to retrieve environment-metadata");
            }
        }
    }
}
