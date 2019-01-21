using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Dto;

namespace Bechtle.A365.ConfigService.Services
{
    /// <summary>
    ///     read projected Environments
    /// </summary>
    public interface IEnvironmentProjectionStore
    {
        /// <summary>
        ///     get a list of projected Environments
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        Task<Result<IList<EnvironmentIdentifier>>> GetAvailable(QueryRange range);

        /// <summary>
        ///     get a list of projected Environments within a category
        /// </summary>
        /// <param name="category"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        Task<Result<IList<EnvironmentIdentifier>>> GetAvailableInCategory(string category, QueryRange range);

        /// <summary>
        ///     get the keys of an Environment as Objects
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        Task<Result<IEnumerable<DtoConfigKey>>> GetKeyObjects(EnvironmentIdentifier identifier, QueryRange range);

        /// <summary>
        ///     get the keys of an Environment as Objects
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="filter"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        Task<Result<IEnumerable<DtoConfigKey>>> GetKeyObjects(EnvironmentIdentifier identifier, string filter, QueryRange range);

        /// <summary>
        ///     get the keys of an Environment
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        Task<Result<IDictionary<string, string>>> GetKeys(EnvironmentIdentifier identifier, QueryRange range);

        /// <summary>
        ///     get the keys of an Environment
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="filter"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        Task<Result<IDictionary<string, string>>> GetKeys(EnvironmentIdentifier identifier, string filter, QueryRange range);

        /// <summary>
        ///     get a list of possible next terms for the given key
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="key"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        Task<Result<IList<DtoConfigKeyCompletion>>> GetKeyAutoComplete(EnvironmentIdentifier identifier, string key, QueryRange range);
    }
}