using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;

namespace Bechtle.A365.ConfigService.Interfaces
{
    /// <summary>
    ///     Component that provides read/write access for the various Domain-Objects
    /// </summary>
    public interface IDomainObjectManager
    {
        /// <summary>
        ///     Assign a ordered list of Layers to a given Environment
        /// </summary>
        /// <param name="environmentIdentifier">valid identifier for an existing Environment</param>
        /// <param name="layerIdentifiers">list of valid identifiers for existing Layers</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult> AssignEnvironmentLayers(
            EnvironmentIdentifier environmentIdentifier,
            IList<LayerIdentifier> layerIdentifiers,
            CancellationToken cancellationToken);

        /// <summary>
        ///     Clone a given Layer with all its data, and a new identifier
        /// </summary>
        /// <param name="sourceIdentifier">Id of the Source-Layer</param>
        /// <param name="targetIdentifier">Id of the new Target-Layer</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult> CloneLayer(LayerIdentifier sourceIdentifier, LayerIdentifier targetIdentifier, CancellationToken cancellationToken);

        /// <summary>
        ///     Create a new Configuration with the given identifier
        /// </summary>
        /// <param name="identifier">valid identifier for a Configuration</param>
        /// <param name="validFrom">start-date from which the configuration should be valid</param>
        /// <param name="validTo">end-date until which the configuration should be valid</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult> CreateConfiguration(
            ConfigurationIdentifier identifier,
            DateTime? validFrom,
            DateTime? validTo,
            CancellationToken cancellationToken);

        /// <summary>
        ///     Create a new Environment with the given identifier
        /// </summary>
        /// <param name="identifier">valid identifier for an Environment that does not yet exist</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult> CreateEnvironment(EnvironmentIdentifier identifier, CancellationToken cancellationToken);

        /// <summary>
        ///     Create a new Environment with the given identifier
        /// </summary>
        /// <param name="identifier">valid identifier for an Environment that does not yet exist</param>
        /// <param name="isDefault">create environment as Default-Environment</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult> CreateEnvironment(EnvironmentIdentifier identifier, bool isDefault, CancellationToken cancellationToken);

        /// <summary>
        ///     Create a new Layer with the given identifier
        /// </summary>
        /// <param name="identifier">valid identifier for a Layer that does not yet exist</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult> CreateLayer(LayerIdentifier identifier, CancellationToken cancellationToken);

        /// <summary>
        ///     Create a new Structure with the given
        /// </summary>
        /// <param name="identifier">valid identifier for the a Structure that does not yet exist</param>
        /// <param name="keys">Dictionary of Keys permanently assigned to this specific Structure</param>
        /// <param name="variables">Dictionary of Variables assigned to this specific Structure</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult> CreateStructure(
            StructureIdentifier identifier,
            IDictionary<string, string> keys,
            IDictionary<string, string> variables,
            CancellationToken cancellationToken);

        /// <summary>
        ///     Remove a single stored Environment
        /// </summary>
        /// <param name="identifier">valid identifier for an existing Environment</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult> DeleteEnvironment(EnvironmentIdentifier identifier, CancellationToken cancellationToken);

        /// <summary>
        ///     Remove a single stored Layer
        /// </summary>
        /// <param name="identifier">valid identifier for an existing Layer</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult> DeleteLayer(LayerIdentifier identifier, CancellationToken cancellationToken);

        /// <summary>
        ///     Get a single stored Configuration
        /// </summary>
        /// <param name="identifier">valid identifier for the Configuration</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult<PreparedConfiguration>> GetConfiguration(ConfigurationIdentifier identifier, CancellationToken cancellationToken);

        /// <summary>
        ///     Get a single stored Configuration
        /// </summary>
        /// <param name="identifier">valid identifier for the Configuration</param>
        /// <param name="version">version of the Object to retrieve</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult<PreparedConfiguration>> GetConfiguration(ConfigurationIdentifier identifier, long version, CancellationToken cancellationToken);

        /// <summary>
        ///     Get a list of all stored Configuration-ids
        /// </summary>
        /// <param name="range">range of ids to retrieve</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult<IList<ConfigurationIdentifier>>> GetConfigurations(QueryRange range, CancellationToken cancellationToken);

        /// <summary>
        ///     Get a list of all stored Configuration-ids
        /// </summary>
        /// <param name="structure">constrain results to those that have this structure</param>
        /// <param name="range">range of ids to retrieve</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult<IList<ConfigurationIdentifier>>> GetConfigurations(StructureIdentifier structure, QueryRange range, CancellationToken cancellationToken);

        /// <summary>
        ///     Get a list of all stored Configuration-ids
        /// </summary>
        /// <param name="environment">constrain results to those that have this environment</param>
        /// <param name="range">range of ids to retrieve</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult<IList<ConfigurationIdentifier>>> GetConfigurations(
            EnvironmentIdentifier environment,
            QueryRange range,
            CancellationToken cancellationToken);

        /// <summary>
        ///     Get a list of all stored Configuration-ids
        /// </summary>
        /// <param name="range">range of ids to retrieve</param>
        /// <param name="version">version from which to list objects</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult<IList<ConfigurationIdentifier>>> GetConfigurations(QueryRange range, long version, CancellationToken cancellationToken);

        /// <summary>
        ///     Get a list of all stored Configuration-ids
        /// </summary>
        /// <param name="version">version from which to list objects</param>
        /// <param name="range">range of ids to retrieve</param>
        /// <param name="structure">constrain results to those that have this structure</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult<IList<ConfigurationIdentifier>>> GetConfigurations(
            StructureIdentifier structure,
            QueryRange range,
            long version,
            CancellationToken cancellationToken);

        /// <summary>
        ///     Get a list of all stored Configuration-ids
        /// </summary>
        /// <param name="environment">constrain results to those that have this environment</param>
        /// <param name="range">range of ids to retrieve</param>
        /// <param name="version">version from which to list objects</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult<IList<ConfigurationIdentifier>>> GetConfigurations(
            EnvironmentIdentifier environment,
            QueryRange range,
            long version,
            CancellationToken cancellationToken);

        /// <summary>
        ///     Get a single stored Environment
        /// </summary>
        /// <param name="identifier">valid identifier for the Environment</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult<ConfigEnvironment>> GetEnvironment(EnvironmentIdentifier identifier, CancellationToken cancellationToken);

        /// <summary>
        ///     Get a single stored Environment
        /// </summary>
        /// <param name="identifier">valid identifier for the Environment</param>
        /// <param name="version">version of the Object to retrieve</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult<ConfigEnvironment>> GetEnvironment(EnvironmentIdentifier identifier, long version, CancellationToken cancellationToken);

        /// <summary>
        ///     Get a list of all stored Environment-ids
        /// </summary>
        /// <param name="range">range of ids to retrieve</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult<IList<EnvironmentIdentifier>>> GetEnvironments(QueryRange range, CancellationToken cancellationToken);

        /// <summary>
        ///     Get a list of all stored Environment-ids
        /// </summary>
        /// <param name="range">range of ids to retrieve</param>
        /// <param name="version">version from which to list objects</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult<IList<EnvironmentIdentifier>>> GetEnvironments(QueryRange range, long version, CancellationToken cancellationToken);

        /// <summary>
        ///     Get a single stored Layer
        /// </summary>
        /// <param name="identifier">valid identifier for the Layer</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult<EnvironmentLayer>> GetLayer(LayerIdentifier identifier, CancellationToken cancellationToken);

        /// <summary>
        ///     Get a single stored Layer
        /// </summary>
        /// <param name="identifier">valid identifier for the Layer</param>
        /// <param name="version">version of the Object to retrieve</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult<EnvironmentLayer>> GetLayer(LayerIdentifier identifier, long version, CancellationToken cancellationToken);

        /// <summary>
        ///     Get a all stored Layer-ids
        /// </summary>
        /// <param name="range">range of ids to retrieve</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult<IList<LayerIdentifier>>> GetLayers(QueryRange range, CancellationToken cancellationToken);

        /// <summary>
        ///     Get a all stored Layer-ids
        /// </summary>
        /// <param name="range">range of ids to retrieve</param>
        /// <param name="version">version at which to list objects</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult<IList<LayerIdentifier>>> GetLayers(QueryRange range, long version, CancellationToken cancellationToken);

        /// <summary>
        ///     Get a list of Configurations that were marked as stale by recent changes
        /// </summary>
        /// <param name="range">range of ids to retrieve</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult<IList<ConfigurationIdentifier>>> GetStaleConfigurations(QueryRange range, CancellationToken cancellationToken);

        /// <summary>
        ///     Get a single stored Structure
        /// </summary>
        /// <param name="identifier">valid identifier for the Structure</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult<ConfigStructure>> GetStructure(StructureIdentifier identifier, CancellationToken cancellationToken);

        /// <summary>
        ///     Get a single stored Structure
        /// </summary>
        /// <param name="identifier">valid identifier for the Structure</param>
        /// <param name="version">version of the Object to retrieve</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult<ConfigStructure>> GetStructure(StructureIdentifier identifier, long version, CancellationToken cancellationToken);

        /// <summary>
        ///     Get a list of all stored Structure-ids
        /// </summary>
        /// <param name="range">range of ids to retrieve</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult<IList<StructureIdentifier>>> GetStructures(QueryRange range, CancellationToken cancellationToken);

        /// <summary>
        ///     Get a list of all stored Structure-ids
        /// </summary>
        /// <param name="name">name of the structure to list</param>
        /// <param name="range">range of ids to retrieve</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult<IList<StructureIdentifier>>> GetStructures(string name, QueryRange range, CancellationToken cancellationToken);

        /// <summary>
        ///     Get a list of all stored Structure-ids
        /// </summary>
        /// <param name="range">range of ids to retrieve</param>
        /// <param name="version">version at which to list objects</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult<IList<StructureIdentifier>>> GetStructures(QueryRange range, long version, CancellationToken cancellationToken);

        /// <summary>
        ///     Get a list of all stored Structure-ids
        /// </summary>
        /// <param name="name">name of the structure to list</param>
        /// <param name="range">range of ids to retrieve</param>
        /// <param name="version">version at which to list objects</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult<IList<StructureIdentifier>>> GetStructures(string name, QueryRange range, long version, CancellationToken cancellationToken);

        /// <summary>
        ///     Import a Layer with the given Keys.
        ///     Will create Layer if it doesn't exist.
        ///     Will overwrite any existing keys.
        /// </summary>
        /// <param name="identifier">valid identifier for an <see cref="EnvironmentLayer" /></param>
        /// <param name="keys">list of keys to assign to this Layer</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>result of the operation</returns>
        Task<IResult> ImportLayer(LayerIdentifier identifier, IList<EnvironmentLayerKey> keys, CancellationToken cancellationToken);

        /// <summary>
        ///     Check if the given Configuration is marked as stale by recent changes
        /// </summary>
        /// <param name="identifier">valid identifier for an existing Configuration</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult<bool>> IsStale(ConfigurationIdentifier identifier);

        /// <summary>
        ///     Modify the keys of a Layer
        /// </summary>
        /// <param name="identifier">valid identifier for an existing Layer</param>
        /// <param name="actions">list of actions to apply to the layers' keys</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult> ModifyLayerKeys(LayerIdentifier identifier, IList<ConfigKeyAction> actions, CancellationToken cancellationToken);

        /// <summary>
        ///     Modify the Variables of a Structure
        /// </summary>
        /// <param name="identifier">valid identifier for an existing Structure</param>
        /// <param name="actions">list of actions to apply to the structures' variables</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult> ModifyStructureVariables(StructureIdentifier identifier, IList<ConfigKeyAction> actions, CancellationToken cancellationToken);

        /// <summary>
        ///     Modify the assigned Tags of a Layer
        /// </summary>
        /// <param name="identifier">valid identifier for an existing Layer</param>
        /// <param name="addedTags">tags to be added to the Layer</param>
        /// <param name="removedTags">tags to be removed from the Layer</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        public Task<IResult> UpdateTags(
            LayerIdentifier identifier,
            IEnumerable<string> addedTags,
            IEnumerable<string> removedTags,
            CancellationToken cancellationToken);
    }
}
