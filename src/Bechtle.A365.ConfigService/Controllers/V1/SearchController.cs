﻿using System;
using System.Net;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Objects;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers.V1
{
    /// <summary>
    ///     search through the projected data
    /// </summary>
    [Route(ApiBaseRoute + "search")]
    public class SearchController : ControllerBase
    {
        private readonly IProjectionStore _store;

        /// <inheritdoc />
        public SearchController(
            ILogger<SearchController> logger,
            IProjectionStore store)
            : base(logger)
        {
            _store = store;
        }

        /// <summary>
        ///     get a list of possible next options from the given query
        /// </summary>
        /// <param name="category">Environment-Category to use for the query</param>
        /// <param name="name">Environment-Name to use for the query</param>
        /// <param name="query">search-query, written as incomplete path</param>
        /// <param name="offset">0-based offset from the beginning of the search-result</param>
        /// <param name="length">query-result size</param>
        /// <param name="targetVersion">Event-Version to use for this operation</param>
        /// <returns>list of Keys that match the given query</returns>
        [ProducesResponseType(typeof(DtoConfigKeyCompletion), (int)HttpStatusCode.OK)]
        [HttpGet("environment/{category}/{name}/keys/autocomplete", Name = "GetEnvironmentKeyAutocomplete")]
        [ApiVersion(ApiVersions.V1, Deprecated = ApiDeprecation.V1)]
        public async Task<IActionResult> GetEnvironmentKeyAutocompleteList(
            [FromRoute] string category,
            [FromRoute] string name,
            [FromQuery] string? query = null,
            [FromQuery] int offset = -1,
            [FromQuery] int length = -1,
            [FromQuery] long targetVersion = -1)
        {
            try
            {
                var range = QueryRange.Make(offset, length);

                var identifier = new EnvironmentIdentifier(category, name);

                var result = await _store.Environments.GetKeyAutoComplete(identifier, query, range, targetVersion);

                if (result.IsError)
                    return ProviderError(result);
                return Ok(result.CheckedData.Items);
            }
            catch (Exception e)
            {
                KnownMetrics.Exception.WithLabels(e.GetType().Name).Inc();
                Logger.LogError(
                    e,
                    "failed to retrieve autocomplete-data ("
                    + "Category='{Category}'; Name='{Name}'; Query='{Query}'; "
                    + "Offset='{Offset}'; Length='{Length}'; TargetVersion='{TargetVersion}')",
                    category,
                    name,
                    query,
                    offset,
                    length,
                    targetVersion);
                return StatusCode(HttpStatusCode.InternalServerError, "failed to retrieve autocomplete-data");
            }
        }

        /// <summary>
        ///     get a list of possible next options from the given query
        /// </summary>
        /// <param name="category">Environment-Category to use for the query</param>
        /// <param name="name">Environment-Name to use for the query</param>
        /// <param name="query">search-query, written as incomplete path</param>
        /// <param name="offset">0-based offset from the beginning of the search-result</param>
        /// <param name="length">query-result size</param>
        /// <param name="targetVersion">Event-Version to use for this operation</param>
        /// <returns>list of Keys that match the given query</returns>
        [ProducesResponseType(typeof(DtoConfigKeyCompletion), (int)HttpStatusCode.OK)]
        [HttpGet("environment/{category}/{name}/keys/autocomplete", Name = "GetEnvironmentKeyAutocompletePaged")]
        [ApiVersion(ApiVersions.V11, Deprecated = ApiDeprecation.V11)]
        public async Task<IActionResult> GetEnvironmentKeyAutocompleteListPaged(
            [FromRoute] string category,
            [FromRoute] string name,
            [FromQuery] string? query = null,
            [FromQuery] int offset = -1,
            [FromQuery] int length = -1,
            [FromQuery] long targetVersion = -1)
        {
            try
            {
                return Result(
                    await _store.Environments.GetKeyAutoComplete(
                        new EnvironmentIdentifier(category, name),
                        query,
                        QueryRange.Make(offset, length),
                        targetVersion));
            }
            catch (Exception e)
            {
                KnownMetrics.Exception.WithLabels(e.GetType().Name).Inc();
                Logger.LogError(
                    e,
                    "failed to retrieve autocomplete-data ("
                    + "Category='{Category}'; Name='{Name}'; Query='{Query}'; "
                    + "Offset='{Offset}'; Length='{Length}'; TargetVersion='{TargetVersion}')",
                    category,
                    name,
                    query,
                    offset,
                    length,
                    targetVersion);
                return StatusCode(HttpStatusCode.InternalServerError, "failed to retrieve autocomplete-data");
            }
        }

        /// <summary>
        ///     get a list of possible next options from the given query
        /// </summary>
        /// <param name="name">Layer-Name to use for the query</param>
        /// <param name="query">search-query, written as incomplete path</param>
        /// <param name="offset">0-based offset from the beginning of the search-result</param>
        /// <param name="length">query-result size</param>
        /// <param name="targetVersion">Event-Version to use for this operation</param>
        /// <returns>list of Keys that match the given query</returns>
        [ProducesResponseType(typeof(DtoConfigKeyCompletion), (int)HttpStatusCode.OK)]
        [HttpGet("layer/{name}/keys/autocomplete", Name = "GetLayerKeyAutocomplete")]
        [ApiVersion(ApiVersions.V1, Deprecated = ApiDeprecation.V1)]
        public async Task<IActionResult> GetLayerKeyAutocompleteList(
            [FromRoute] string name,
            [FromQuery] string? query = null,
            [FromQuery] int offset = -1,
            [FromQuery] int length = -1,
            [FromQuery] long targetVersion = -1)
        {
            try
            {
                var range = QueryRange.Make(offset, length);

                var identifier = new LayerIdentifier(name);

                var result = await _store.Layers.GetKeyAutoComplete(identifier, query, range, targetVersion);

                if (result.IsError)
                    return ProviderError(result);
                return Ok(result.CheckedData.Items);
            }
            catch (Exception e)
            {
                KnownMetrics.Exception.WithLabels(e.GetType().Name).Inc();
                Logger.LogError(
                    e,
                    "failed to retrieve autocomplete-data ("
                    + "Name='{Name}'; Query='{Query}'; "
                    + "Offset='{Offset}'; Length='{Length}'; TargetVersion='{TargetVersion}')",
                    name,
                    query,
                    offset,
                    length,
                    targetVersion);
                return StatusCode(HttpStatusCode.InternalServerError, "failed to retrieve autocomplete-data");
            }
        }

        /// <summary>
        ///     get a list of possible next options from the given query
        /// </summary>
        /// <param name="name">Layer-Name to use for the query</param>
        /// <param name="query">search-query, written as incomplete path</param>
        /// <param name="offset">0-based offset from the beginning of the search-result</param>
        /// <param name="length">query-result size</param>
        /// <param name="targetVersion">Event-Version to use for this operation</param>
        /// <returns>list of Keys that match the given query</returns>
        [ProducesResponseType(typeof(DtoConfigKeyCompletion), (int)HttpStatusCode.OK)]
        [HttpGet("layer/{name}/keys/autocomplete", Name = "GetLayerKeyAutocompletePaged")]
        [ApiVersion(ApiVersions.V11, Deprecated = ApiDeprecation.V11)]
        public async Task<IActionResult> GetLayerKeyAutocompleteListPaged(
            [FromRoute] string name,
            [FromQuery] string? query = null,
            [FromQuery] int offset = -1,
            [FromQuery] int length = -1,
            [FromQuery] long targetVersion = -1)
        {
            try
            {
                return Result(
                    await _store.Layers.GetKeyAutoComplete(
                        new LayerIdentifier(name),
                        query,
                        QueryRange.Make(offset, length),
                        targetVersion));
            }
            catch (Exception e)
            {
                KnownMetrics.Exception.WithLabels(e.GetType().Name).Inc();
                Logger.LogError(
                    e,
                    "failed to retrieve autocomplete-data ("
                    + "Name='{Name}'; Query='{Query}'; "
                    + "Offset='{Offset}'; Length='{Length}'; TargetVersion='{TargetVersion}')",
                    name,
                    query,
                    offset,
                    length,
                    targetVersion);
                return StatusCode(HttpStatusCode.InternalServerError, "failed to retrieve autocomplete-data");
            }
        }
    }
}
