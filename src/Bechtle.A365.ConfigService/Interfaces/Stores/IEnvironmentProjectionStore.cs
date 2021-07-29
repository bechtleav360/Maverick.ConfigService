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
    ///     read projected Environments
    /// </summary>
    public interface IEnvironmentProjectionStore : IDisposable, IAsyncDisposable
    {
        /// <summary>
        ///     assign the given list of layers in the given order to an Environment
        /// </summary>
        /// <param name="identifier">Id pointing to the Environment to operate on</param>
        /// <param name="layers">list of layers to assign to this Environment</param>
        /// <returns></returns>
        Task<IResult> AssignLayers(EnvironmentIdentifier identifier, IList<LayerIdentifier> layers);

        /// <summary>
        ///     create a new Environment with the given <see cref="EnvironmentIdentifier" />
        /// </summary>
        /// <param name="identifier">Id pointing to the Environment to operate on</param>
        /// <param name="isDefault">flag indicating if this Environment should be created as Default-Env</param>
        /// <returns></returns>
        Task<IResult> Create(EnvironmentIdentifier identifier, bool isDefault);

        /// <summary>
        ///     delete an existing Environment with the given <see cref="EnvironmentIdentifier" />
        /// </summary>
        /// <param name="identifier">Id pointing to the Environment to operate on</param>
        /// <returns></returns>
        Task<IResult> Delete(EnvironmentIdentifier identifier);

        /// <summary>
        ///     get the ordered list of assigned layers for the given <see cref="EnvironmentIdentifier" />
        /// </summary>
        /// <param name="identifier">Id pointing to the Environment to operate on</param>
        /// <returns></returns>
        Task<IResult<IList<LayerIdentifier>>> GetAssignedLayers(EnvironmentIdentifier identifier);

        /// <summary>
        ///     get the ordered list of assigned layers for the given <see cref="EnvironmentIdentifier" /> at the given Version
        /// </summary>
        /// <param name="identifier">Id pointing to the Environment to operate on</param>
        /// <param name="version">Event-Version to use when streaming Objects</param>
        /// <returns></returns>
        Task<IResult<IList<LayerIdentifier>>> GetAssignedLayers(EnvironmentIdentifier identifier, long version);

        /// <summary>
        ///     get a list of all Environments
        /// </summary>
        /// <param name="range">Pagination-Information</param>
        /// <returns></returns>
        Task<IResult<IList<EnvironmentIdentifier>>> GetAvailable(QueryRange range);

        /// <summary>
        ///     get a list of all Environments
        /// </summary>
        /// <param name="range">Pagination-Information</param>
        /// <param name="version">Event-Version to use when streaming Objects</param>
        /// <returns></returns>
        Task<IResult<IList<EnvironmentIdentifier>>> GetAvailable(QueryRange range, long version);

        /// <summary>
        ///     get a list of possible next terms for the given key
        /// </summary>
        /// <param name="identifier">Id pointing to the Environment to operate on</param>
        /// <param name="key">current key used to see which keys sibling / children are available</param>
        /// <param name="range">Pagination-Information</param>
        /// <returns></returns>
        Task<IResult<IList<DtoConfigKeyCompletion>>> GetKeyAutoComplete(EnvironmentIdentifier identifier, string key, QueryRange range);

        /// <summary>
        ///     get a list of possible next terms for the given key
        /// </summary>
        /// <param name="identifier">Id pointing to the Environment to operate on</param>
        /// <param name="key">current key used to see which keys sibling / children are available</param>
        /// <param name="range">Pagination-Information</param>
        /// <param name="version">Event-Version to use when streaming Objects</param>
        /// <returns></returns>
        Task<IResult<IList<DtoConfigKeyCompletion>>> GetKeyAutoComplete(EnvironmentIdentifier identifier, string key, QueryRange range, long version);

        /// <summary>
        ///     get the keys of an Environment as Objects
        /// </summary>
        /// <param name="parameters">query-parameters to use when reading keys</param>
        /// <returns></returns>
        Task<IResult<IEnumerable<DtoConfigKey>>> GetKeyObjects(KeyQueryParameters<EnvironmentIdentifier> parameters);

        /// <summary>
        ///     get the keys of an Environment
        /// </summary>
        /// <param name="parameters">query-parameters to use when reading keys</param>
        /// <returns></returns>
        Task<IResult<IDictionary<string, string>>> GetKeys(KeyQueryParameters<EnvironmentIdentifier> parameters);

        /// <summary>
        ///     get all available Metadata for an Environment
        /// </summary>
        /// <param name="identifier">Id pointing to the Environment to operate on</param>
        /// <returns>result of the operation the desired Metadata</returns>
        Task<IResult<ConfigEnvironmentMetadata>> GetMetadata(EnvironmentIdentifier identifier);
    }
}
