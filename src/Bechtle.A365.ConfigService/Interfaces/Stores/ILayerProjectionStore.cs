using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Objects;
using Bechtle.A365.ConfigService.Implementations;
using Bechtle.A365.ConfigService.Models.V1;

namespace Bechtle.A365.ConfigService.Interfaces.Stores
{
    /// <summary>
    ///     read projected Layers
    /// </summary>
    public interface ILayerProjectionStore : IDisposable, IAsyncDisposable
    {
        /// <summary>
        ///     create a new Layer from existing data.
        /// </summary>
        /// <param name="sourceId">id of the source-layer</param>
        /// <param name="targetId">id of the newly created layer</param>
        /// <returns>Result of the operation</returns>
        Task<IResult> Clone(LayerIdentifier sourceId, LayerIdentifier targetId);

        /// <summary>
        ///     create a new Layer with the given <see cref="LayerIdentifier" />
        /// </summary>
        /// <param name="identifier">Id of the layer that should be created</param>
        /// <returns>Result of the operation</returns>
        Task<IResult> Create(LayerIdentifier identifier);

        /// <summary>
        ///     delete an existing Layer with the given <see cref="LayerIdentifier" />
        /// </summary>
        /// <param name="identifier">Id of the layer that should be deleted</param>
        /// <returns>Result of the operation</returns>
        Task<IResult> Delete(LayerIdentifier identifier);

        /// <summary>
        ///     remove a number of keys from the given Layer
        /// </summary>
        /// <param name="identifier">Id of the layer whose keys should be removed</param>
        /// <param name="keysToDelete">list of keys that should be removed</param>
        /// <returns>Result of the operation</returns>
        Task<IResult> DeleteKeys(LayerIdentifier identifier, ICollection<string> keysToDelete);

        /// <summary>
        ///     get a list of all Layers
        /// </summary>
        /// <param name="range">paging-information for the returned list</param>
        /// <returns>Result of the operation</returns>
        Task<IResult<Page<LayerIdentifier>>> GetAvailable(QueryRange range);

        /// <summary>
        ///     get a list of all Layers
        /// </summary>
        /// <param name="range">paging-information for the returned list</param>
        /// <param name="version">Maximum version within the database to consider for the returned objects</param>
        /// <returns>Result of the operation</returns>
        Task<IResult<Page<LayerIdentifier>>> GetAvailable(QueryRange range, long version);

        /// <summary>
        ///     get a list of possible next terms for the given key
        /// </summary>
        /// <param name="identifier">Id of the layer to retrieve autocomplete-info for</param>
        /// <param name="key">current path to suggest options for</param>
        /// <param name="range">paging-information for the returned list</param>
        /// <returns>Result of the operation</returns>
        Task<IResult<Page<DtoConfigKeyCompletion>>> GetKeyAutoComplete(LayerIdentifier identifier, string key, QueryRange range);

        /// <summary>
        ///     get a list of possible next terms for the given key
        /// </summary>
        /// <param name="identifier">Id of the layer to retrieve autocomplete-info for</param>
        /// <param name="key">current path to suggest options for</param>
        /// <param name="range">paging-information for the returned list</param>
        /// <param name="version">Maximum version within the database to consider for the returned list</param>
        /// <returns>Result of the operation</returns>
        Task<IResult<Page<DtoConfigKeyCompletion>>> GetKeyAutoComplete(LayerIdentifier identifier, string key, QueryRange range, long version);

        /// <summary>
        ///     get the keys of an Layer as Objects
        /// </summary>
        /// <param name="parameters">structured parameters to retrieve keys for a given Layer</param>
        /// <returns>Result of the operation</returns>
        Task<IResult<Page<DtoConfigKey>>> GetKeyObjects(KeyQueryParameters<LayerIdentifier> parameters);

        /// <summary>
        ///     get the keys of an Layer
        /// </summary>
        /// <param name="parameters">structured parameters to retrieve keys for a given Layer</param>
        /// <returns>Result of the operation</returns>
        Task<IResult<Page<KeyValuePair<string, string>>>> GetKeys(KeyQueryParameters<LayerIdentifier> parameters);

        /// <summary>
        ///     get metadata for a Layer
        /// </summary>
        /// <param name="identifier">Id of the layer to retrieve metadata for</param>
        /// <returns>Result of the operation</returns>
        Task<IResult<EnvironmentLayerMetadata>> GetMetadata(LayerIdentifier identifier);

        /// <summary>
        ///     Get a list of Tags assigned to the given Layer
        /// </summary>
        /// <param name="identifier">Id of the layer to update the tags of</param>
        /// <returns>Result of the operation</returns>
        Task<IResult<Page<string>>> GetTags(LayerIdentifier identifier);

        /// <summary>
        ///     add or update a number of keys in the given Layer
        /// </summary>
        /// <param name="identifier">Id of the layer to update keys for</param>
        /// <param name="keys">list of Updates to apply to the existing keys</param>
        /// <returns>Result of the operation</returns>
        Task<IResult> UpdateKeys(LayerIdentifier identifier, ICollection<DtoConfigKey> keys);

        /// <summary>
        ///     Update the assigned Tags for a Layer
        /// </summary>
        /// <param name="identifier">Id of the layer to update the tags of</param>
        /// <param name="addedTags">list of tags to be added to the Layer</param>
        /// <param name="removedTags">list of tags to be removed from the Layer</param>
        /// <returns>Result of the operation</returns>
        public Task<IResult> UpdateTags(LayerIdentifier identifier, ICollection<string> addedTags, ICollection<string> removedTags);
    }
}
