using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DbObjects;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.Projection.DataStorage
{
    /// <summary>
    /// </summary>
    public interface IConfigurationDatabase
    {
        /// <summary>
        ///     append a new metadata-object to the list of event-metadata
        /// </summary>
        /// <param name="metadata"></param>
        /// <returns></returns>
        Task<IResult> AppendProjectedEventMetadata(ProjectedEventMetadata metadata);

        /// <summary>
        ///     apply a set of changes to an Environment identified by <paramref name="identifier" />
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="actions"></param>
        /// <returns></returns>
        Task<IResult> ApplyChanges(EnvironmentIdentifier identifier, IList<ConfigKeyAction> actions);

        /// <summary>
        ///     apply a set of changes to a Structures Variables
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="actions"></param>
        /// <returns></returns>
        Task<IResult> ApplyChanges(StructureIdentifier identifier, IList<ConfigKeyAction> actions);

        /// <summary>
        ///     connect to the (possibly) remote database
        /// </summary>
        /// <returns></returns>
        Task<IResult> Connect();

        /// <summary>
        ///     create a new Environment with the given identifier
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="defaultEnvironment"></param>
        /// <returns></returns>
        Task<IResult> CreateEnvironment(EnvironmentIdentifier identifier, bool defaultEnvironment);

        /// <summary>
        ///     create a new Structure with the given identifier
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="keys"></param>
        /// <param name="variables"></param>
        /// <returns></returns>
        Task<IResult> CreateStructure(StructureIdentifier identifier, IDictionary<string, string> keys, IDictionary<string, string> variables);

        /// <summary>
        ///     delete an existing Environment with the given identifier
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<IResult> DeleteEnvironment(EnvironmentIdentifier identifier);

        /// <summary>
        ///     delete an existing Structure with the given identifier
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<IResult> DeleteStructure(StructureIdentifier identifier);

        /// <summary>
        ///     retrieve all current keys for the given environment and generate Auto-Complete data for it
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<IResult> GenerateEnvironmentKeyAutocompleteData(EnvironmentIdentifier identifier);

        /// <summary>
        ///     get all data for the Default-Environment for the given category
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        Task<IResult<EnvironmentSnapshot>> GetDefaultEnvironment(string category);

        /// <summary>
        ///     get all data for the specified Environment
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<IResult<EnvironmentSnapshot>> GetEnvironment(EnvironmentIdentifier identifier);

        /// <summary>
        ///     get all data for the specified Environment, and inherit keys from the Default Environment
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<IResult<EnvironmentSnapshot>> GetEnvironmentWithInheritance(EnvironmentIdentifier identifier);

        /// <summary>
        ///     get the Identifier of the latest active Configuration
        /// </summary>
        /// <returns></returns>
        Task<ConfigurationIdentifier> GetLatestActiveConfiguration();

        /// <summary>
        ///     get the id of the last projected event.
        /// </summary>
        /// <returns></returns>
        Task<long?> GetLatestProjectedEventId();

        /// <summary>
        ///     get a list of metadata-objects for all projected events
        /// </summary>
        /// <returns></returns>
        Task<IResult<IList<ProjectedEventMetadata>>> GetProjectedEventMetadata();

        /// <summary>
        ///     get the structure with default-values for the specified Structure
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<IResult<StructureSnapshot>> GetStructure(StructureIdentifier identifier);

        /// <summary>
        ///     Replace all keys with the given keys, remove spare keys
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="actions"></param>
        /// <returns></returns>
        Task<IResult> ImportEnvironment(EnvironmentIdentifier identifier, IList<ConfigKeyAction> actions);

        /// <summary>
        ///     save a configuration compiled from <paramref name="environment" /> and <paramref name="structure" />
        /// </summary>
        /// <param name="environment"></param>
        /// <param name="structure"></param>
        /// <param name="configuration"></param>
        /// <param name="configurationJson"></param>
        /// <param name="usedKeys"></param>
        /// <param name="validFrom"></param>
        /// <param name="validTo"></param>
        /// <returns></returns>
        Task<IResult> SaveConfiguration(EnvironmentSnapshot environment,
                                        StructureSnapshot structure,
                                        IDictionary<string, string> configuration,
                                        string configurationJson,
                                        IEnumerable<string> usedKeys,
                                        DateTime? validFrom,
                                        DateTime? validTo);

        /// <summary>
        ///     set the Identifier of the latest active Configuration
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task SetLatestActiveConfiguration(ConfigurationIdentifier identifier);

        /// <summary>
        ///     save the id of the last projected event.
        /// </summary>
        /// <returns></returns>
        Task SetLatestProjectedEventId(long latestEventId);
    }
}