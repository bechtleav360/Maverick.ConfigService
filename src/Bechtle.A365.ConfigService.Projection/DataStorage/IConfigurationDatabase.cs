using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;

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
        /// <param name="keys"></param>
        /// <param name="variables"></param>
        /// <returns></returns>
        Task<Result> CreateStructure(StructureIdentifier identifier, IDictionary<string, string> keys, IDictionary<string, string> variables);

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
        Task<Result<EnvironmentSnapshot>> GetEnvironment(EnvironmentIdentifier identifier);

        /// <summary>
        ///     get all data for the specified Environment, and inherit keys from the Default Environment
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<Result<EnvironmentSnapshot>> GetEnvironmentWithInheritance(EnvironmentIdentifier identifier);

        /// <summary>
        ///     get all data for the Default-Environment for the given category
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        Task<Result<EnvironmentSnapshot>> GetDefaultEnvironment(string category);

        /// <summary>
        ///     get the id of the last projected event.
        /// </summary>
        /// <returns></returns>
        Task<long?> GetLatestProjectedEventId();

        /// <summary>
        ///     get the structure with default-values for the specified Structure
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<Result<StructureSnapshot>> GetStructure(StructureIdentifier identifier);

        /// <summary>
        ///     save a configuration compiled from <paramref name="environment" /> and <paramref name="structure" />
        /// </summary>
        /// <param name="environment"></param>
        /// <param name="structure"></param>
        /// <param name="configuration"></param>
        /// <param name="configurationJson"></param>
        /// <param name="validFrom"></param>
        /// <param name="validTo"></param>
        /// <returns></returns>
        Task<Result> SaveConfiguration(EnvironmentSnapshot environment,
                                       StructureSnapshot structure,
                                       IDictionary<string, string> configuration,
                                       string configurationJson,
                                       DateTime? validFrom,
                                       DateTime? validTo);

        /// <summary>
        ///     save the id of the last projected event.
        /// </summary>
        /// <returns></returns>
        Task SetLatestProjectedEventId(long latestEventId);
    }
}