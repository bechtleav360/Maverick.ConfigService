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
    public interface IEnvironmentProjectionStore
    {
        /// <summary>
        ///     create a new Environment with the given <see cref="EnvironmentIdentifier" />
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="isDefault"></param>
        /// <returns></returns>
        Task<IResult> Create(EnvironmentIdentifier identifier, bool isDefault);

        /// <summary>
        ///     delete an existing Environment with the given <see cref="EnvironmentIdentifier" />
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<IResult> Delete(EnvironmentIdentifier identifier);

        /// <summary>
        ///     remove a number of keys from the given Environment
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="keysToDelete"></param>
        /// <returns></returns>
        Task<IResult> DeleteKeys(EnvironmentIdentifier identifier, ICollection<string> keysToDelete);

        /// <summary>
        ///     get a list of all Environments
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        Task<IResult<IList<EnvironmentIdentifier>>> GetAvailable(QueryRange range);

        /// <summary>
        ///     get a list of possible next terms for the given key
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="key"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        Task<IResult<IList<DtoConfigKeyCompletion>>> GetKeyAutoComplete(EnvironmentIdentifier identifier, string key, QueryRange range);

        /// <summary>
        ///     get the keys of an Environment as Objects
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        Task<IResult<IEnumerable<DtoConfigKey>>> GetKeyObjects(EnvironmentKeyQueryParameters parameters);

        /// <summary>
        ///     get the keys of an Environment
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        Task<IResult<IDictionary<string, string>>> GetKeys(EnvironmentKeyQueryParameters parameters);

        /// <summary>
        ///     add or update a number of keys in the given Environmenet
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        Task<IResult> UpdateKeys(EnvironmentIdentifier identifier, ICollection<DtoConfigKey> keys);
    }
}