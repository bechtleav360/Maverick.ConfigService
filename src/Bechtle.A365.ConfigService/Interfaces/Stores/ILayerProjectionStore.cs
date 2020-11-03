using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Objects;
using Bechtle.A365.ConfigService.Implementations;

namespace Bechtle.A365.ConfigService.Interfaces.Stores
{
    /// <summary>
    ///     read projected Layers
    /// </summary>
    public interface ILayerProjectionStore : IDisposable, IAsyncDisposable
    {
        /// <summary>
        ///     create a new Layer with the given <see cref="LayerIdentifier" />
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<IResult> Create(LayerIdentifier identifier);

        /// <summary>
        ///     delete an existing Layer with the given <see cref="LayerIdentifier" />
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<IResult> Delete(LayerIdentifier identifier);

        /// <summary>
        ///     get a list of all Layers
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        Task<IResult<IList<LayerIdentifier>>> GetAvailable(QueryRange range);

        /// <summary>
        ///     get a list of all Layers
        /// </summary>
        /// <param name="range"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        Task<IResult<IList<LayerIdentifier>>> GetAvailable(QueryRange range, long version);

        /// <summary>
        ///     get a list of possible next terms for the given key
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="key"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        Task<IResult<IList<DtoConfigKeyCompletion>>> GetKeyAutoComplete(LayerIdentifier identifier, string key, QueryRange range);

        /// <summary>
        ///     get a list of possible next terms for the given key
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="key"></param>
        /// <param name="range"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        Task<IResult<IList<DtoConfigKeyCompletion>>> GetKeyAutoComplete(LayerIdentifier identifier, string key, QueryRange range, long version);

        /// <summary>
        ///     get the keys of an Layer as Objects
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        Task<IResult<IEnumerable<DtoConfigKey>>> GetKeyObjects(KeyQueryParameters<LayerIdentifier> parameters);

        /// <summary>
        ///     get the keys of an Layer
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        Task<IResult<IDictionary<string, string>>> GetKeys(KeyQueryParameters<LayerIdentifier> parameters);
    }
}