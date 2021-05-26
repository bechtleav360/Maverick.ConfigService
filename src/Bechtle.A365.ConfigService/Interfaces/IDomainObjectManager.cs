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
        ///     Get a single stored Environment
        /// </summary>
        /// <param name="identifier">valid identifier for the Environment</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult<ConfigEnvironment>> GetEnvironment(EnvironmentIdentifier identifier, CancellationToken cancellationToken);

        /// <summary>
        ///     Remove a single stored Environment
        /// </summary>
        /// <param name="identifier">valid identifier for an existing Environment</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult<ConfigEnvironment>> DeleteEnvironment(EnvironmentIdentifier identifier, CancellationToken cancellationToken);

        /// <summary>
        ///     Get a single stored Structure
        /// </summary>
        /// <param name="identifier">valid identifier for the Structure</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult<ConfigStructure>> GetStructure(StructureIdentifier identifier, CancellationToken cancellationToken);

        /// <summary>
        ///     Get a single stored Layer
        /// </summary>
        /// <param name="identifier">valid identifier for the Layer</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult<EnvironmentLayer>> GetLayer(LayerIdentifier identifier, CancellationToken cancellationToken);

        /// <summary>
        ///     Remove a single stored Layer
        /// </summary>
        /// <param name="identifier">valid identifier for an existing Layer</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult<EnvironmentLayer>> DeleteLayer(LayerIdentifier identifier, CancellationToken cancellationToken);

        /// <summary>
        ///     Get a single stored Configuration
        /// </summary>
        /// <param name="identifier">valid identifier for the Configuration</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult<PreparedConfiguration>> GetConfiguration(ConfigurationIdentifier identifier, CancellationToken cancellationToken);

        /// <summary>
        ///     Get a single stored Configurations Keys
        /// </summary>
        /// <param name="identifier">valid identifier for the Configuration</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult<IDictionary<string, string>>> GetConfigurationKeys(ConfigurationIdentifier identifier, CancellationToken cancellationToken);

        /// <summary>
        ///     Create a new Environment with the given identifier
        /// </summary>
        /// <param name="identifier">valid identifier for an Environment that does not yet exist</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult> CreateEnvironment(EnvironmentIdentifier identifier, CancellationToken cancellationToken);

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
        ///     Create a new Layer with the given identifier
        /// </summary>
        /// <param name="identifier">valid identifier for a Layer that does not yet exist</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult> CreateLayer(LayerIdentifier identifier, CancellationToken cancellationToken);

        /// <summary>
        ///     Create a new Configuration with the given identifier
        /// </summary>
        /// <param name="identifier">valid identifier for a Configuration</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult> CreateConfiguration(ConfigurationIdentifier identifier, CancellationToken cancellationToken);

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
        ///     Modify the keys of a Layer
        /// </summary>
        /// <param name="identifier">valid identifier for an existing Layer</param>
        /// <param name="actions">list of actions to apply to the layers' keys</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        /// <returns></returns>
        Task<IResult> ModifyLayerKeys(LayerIdentifier identifier, IList<ConfigKeyAction> actions, CancellationToken cancellationToken);

        /// <summary>
        ///     Modify the keys of a Layer
        /// </summary>
        /// <param name="identifier">valid identifier for an existing Layer</param>
        /// <param name="keys">list of that overwrite the current Layer</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        /// <returns></returns>
        Task<IResult> ModifyLayerKeys(LayerIdentifier identifier, IList<EnvironmentLayerKey> keys, CancellationToken cancellationToken);

        /// <summary>
        ///     Modify the Variables of a Structure
        /// </summary>
        /// <param name="identifier">valid identifier for an existing Structure</param>
        /// <param name="actions">list of actions to apply to the structures' variables</param>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns>Result of the Operation</returns>
        Task<IResult> ModifyStructureVariables(StructureIdentifier identifier, IList<ConfigKeyAction> actions, CancellationToken cancellationToken);
    }
}
