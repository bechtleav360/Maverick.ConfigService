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
        /// <returns></returns>
        Task<Result<IList<EnvironmentIdentifier>>> GetAvailable();

        /// <summary>
        ///     get a list of projected Environments within a category
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        Task<Result<IList<EnvironmentIdentifier>>> GetAvailableInCategory(string category);

        /// <summary>
        ///     get the keys of an Environment
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<Result<IDictionary<string, string>>> GetKeys(EnvironmentIdentifier identifier);

        /// <summary>
        ///     get the keys of an Environment, and inheriting keys from Default Environment
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<Result<IDictionary<string, string>>> GetKeysWithInheritance(EnvironmentIdentifier identifier);

        /// <summary>
        ///     get the keys of an Environment as Objects
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<Result<IEnumerable<DtoConfigKey>>> GetKeyObjects(EnvironmentIdentifier identifier);

        /// <summary>
        ///     get the keys of an Environment as Objects, and inheriting keys from Default Environment
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<Result<IEnumerable<DtoConfigKey>>> GetKeyObjectsWithInheritance(EnvironmentIdentifier identifier);
    }
}