using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.Services
{
    /// <summary>
    ///     read projected Configurations
    /// </summary>
    public interface IConfigurationProjectionStore
    {
        /// <summary>
        ///     get a list of available projected Configurations
        /// </summary>
        /// <returns></returns>
        Task<Result<IList<ConfigurationIdentifier>>> GetAvailable();

        /// <summary>
        ///     get a list of available projected Configurations constrained to the specified Environment
        /// </summary>
        /// <param name="environment"></param>
        /// <returns></returns>
        Task<Result<IList<ConfigurationIdentifier>>> GetAvailableWithEnvironment(EnvironmentIdentifier environment);

        /// <summary>
        ///     get a list of available projected Configurations constrained to the specified Structure
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        Task<Result<IList<ConfigurationIdentifier>>> GetAvailableWithStructure(StructureIdentifier structure);

        /// <summary>
        ///     get the keys of a Configuration
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<Result<IDictionary<string, string>>> GetKeys(ConfigurationIdentifier identifier);
    }
}