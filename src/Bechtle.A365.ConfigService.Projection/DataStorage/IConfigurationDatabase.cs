using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Dto;
using Bechtle.A365.ConfigService.Dto.DomainEvents;

namespace Bechtle.A365.ConfigService.Projection.DataStorage
{
    /// <summary>
    /// </summary>
    public interface IConfigurationDatabase
    {
        /// <summary>
        ///     apply a set of changes to an Environment identified by <paramref name="identifier" />
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="actions"></param>
        /// <returns></returns>
        Task<Result> ApplyChanges(EnvironmentIdentifier identifier, IList<ConfigKeyAction> actions);

        /// <summary>
        ///     connect to the (possibly) remote database
        /// </summary>
        /// <returns></returns>
        Task<Result> Connect();

        /// <summary>
        ///     create a new Environment with the given identifier
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="defaultEnvironment"></param>
        /// <returns></returns>
        Task<Result> CreateEnvironment(EnvironmentIdentifier identifier, bool defaultEnvironment);

        /// <summary>
        ///     create a new Structure with the given identifier
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="actions"></param>
        /// <returns></returns>
        Task<Result> CreateStructure(StructureIdentifier identifier, IList<ConfigKeyAction> actions);

        /// <summary>
        ///     delete an existing Environment with the given identifier
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<Result> DeleteEnvironment(EnvironmentIdentifier identifier);

        /// <summary>
        ///     delete an existing Structure with the given identifier
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<Result> DeleteStructure(StructureIdentifier identifier);

        /// <summary>
        ///     get all data for the specified Environment
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<Result<Snapshot<EnvironmentIdentifier>>> GetEnvironment(EnvironmentIdentifier identifier);

        /// <summary>
        ///     get the structure with default-values for the specified Structure
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<Result<Snapshot<StructureIdentifier>>> GetStructure(StructureIdentifier identifier);

        /// <summary>
        ///     save a configuration compiled from <paramref name="environment" /> and <paramref name="structure" />
        /// </summary>
        /// <param name="environment"></param>
        /// <param name="structure"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        Task<Result> SaveConfiguration(Snapshot<EnvironmentIdentifier> environment,
                                       Snapshot<StructureIdentifier> structure,
                                       IDictionary<string, string> configuration);
    }
}