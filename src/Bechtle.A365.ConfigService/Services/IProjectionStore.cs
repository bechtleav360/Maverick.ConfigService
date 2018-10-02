using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Dto.DbObjects;
using Bechtle.A365.ConfigService.Dto.DomainEvents;

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
    }
}