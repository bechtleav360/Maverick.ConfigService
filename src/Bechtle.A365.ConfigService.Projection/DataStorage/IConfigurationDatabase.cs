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
        ///     connect to the (possibly) remote database
        /// </summary>
        /// <returns></returns>
        Task<Result> Connect();

        /// <summary>
        ///     apply a set of changes to an Environment identified by <paramref name="identifier"/>
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="actions"></param>
        /// <returns></returns>
        Task<Result> ApplyChanges(EnvironmentIdentifier identifier, List<ConfigKeyAction> actions);

        /// <summary>
        ///     create a new Environment with the given identifier
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<Result> CreateEnvironment(EnvironmentIdentifier identifier);

        /// <summary>
        ///     delete an existing Environment with the given identifier
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<Result> DeleteEnvironment(EnvironmentIdentifier identifier);

        /// <summary>
        ///     create a new Structure with the given identifier
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<Result> CreateStructure(StructureIdentifier identifier);

        /// <summary>
        ///     delete an existing Structure with the given identifier
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<Result> DeleteStructure(StructureIdentifier identifier);
    }
}