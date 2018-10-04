using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DbObjects;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.Services
{
    /// <summary>
    ///     read projected configurations
    /// </summary>
    public interface IProjectionStore
    {
        /// <summary>
        ///     get a list of all available configurations
        /// </summary>
        /// <returns></returns>
        Task<IDictionary<EnvironmentIdentifier, IList<StructureIdentifier>>> GetAvailableConfigurations();

        /// <summary>
        ///     get a list of available environments
        /// </summary>
        /// <returns></returns>
        Task<IList<EnvironmentIdentifier>> GetAvailableEnvironments();

        /// <summary>
        ///     get a list of available structures
        /// </summary>
        /// <returns></returns>
        Task<IList<StructureIdentifier>> GetAvailableStructures();

        /// <summary>
        ///     get the keys within an Environment
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<IDictionary<string, string>> GetEnvironmentKeys(EnvironmentIdentifier identifier);

        /// <summary>
        ///     get a specific <see cref="ProjectedConfiguration" />
        /// </summary>
        /// <param name="environment"></param>
        /// <param name="structure"></param>
        /// <returns></returns>
        Task<ProjectedConfiguration> GetProjectedConfiguration(EnvironmentIdentifier environment, StructureIdentifier structure);

        /// <summary>
        ///     get the keys within a <see cref="ProjectedConfiguration" /> directly
        /// </summary>
        /// <param name="environment"></param>
        /// <param name="structure"></param>
        /// <returns></returns>
        Task<IDictionary<string, string>> GetProjectedConfigurationKeys(EnvironmentIdentifier environment, StructureIdentifier structure);

        /// <summary>
        ///     get the keys within a structure
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<IDictionary<string, string>> GetStructureKeys(StructureIdentifier identifier);
    }
}