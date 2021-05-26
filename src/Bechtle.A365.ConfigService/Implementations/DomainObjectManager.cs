using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Interfaces.Stores;

namespace Bechtle.A365.ConfigService.Implementations
{
    /// <summary>
    ///     Default-Implementation of <see cref="IDomainObjectManager"/>
    /// </summary>
    public class DomainObjectManager : IDomainObjectManager
    {
        private readonly IDomainObjectStore _objectStore;

        /// <summary>
        ///     Create a new instance of <see cref="DomainObjectManager"/>
        /// </summary>
        /// <param name="objectStore">instance to store/load DomainObjects to/from</param>
        public DomainObjectManager(IDomainObjectStore objectStore)
        {
            _objectStore = objectStore;
        }

        /// <inheritdoc />
        public Task<IResult<ConfigEnvironment>> GetEnvironment(EnvironmentIdentifier identifier, CancellationToken cancellationToken)
            => throw new System.NotImplementedException();

        /// <inheritdoc />
        public Task<IResult<ConfigEnvironment>> DeleteEnvironment(EnvironmentIdentifier identifier, CancellationToken cancellationToken)
            => throw new System.NotImplementedException();

        /// <inheritdoc />
        public Task<IResult<ConfigStructure>> GetStructure(StructureIdentifier identifier, CancellationToken cancellationToken)
            => throw new System.NotImplementedException();

        /// <inheritdoc />
        public Task<IResult<EnvironmentLayer>> GetLayer(LayerIdentifier identifier, CancellationToken cancellationToken)
            => throw new System.NotImplementedException();

        /// <inheritdoc />
        public Task<IResult<EnvironmentLayer>> DeleteLayer(LayerIdentifier identifier, CancellationToken cancellationToken)
            => throw new System.NotImplementedException();

        /// <inheritdoc />
        public Task<IResult<PreparedConfiguration>> GetConfiguration(ConfigurationIdentifier identifier, CancellationToken cancellationToken)
            => throw new System.NotImplementedException();

        /// <inheritdoc />
        public Task<IResult<IDictionary<string, string>>> GetConfigurationKeys(ConfigurationIdentifier identifier, CancellationToken cancellationToken)
            => throw new System.NotImplementedException();

        /// <inheritdoc />
        public Task<IResult> CreateEnvironment(EnvironmentIdentifier identifier, CancellationToken cancellationToken)
            => throw new System.NotImplementedException();

        /// <inheritdoc />
        public Task<IResult> CreateStructure(
            StructureIdentifier identifier,
            IDictionary<string, string> keys,
            IDictionary<string, string> variables,
            CancellationToken cancellationToken)
            => throw new System.NotImplementedException();

        /// <inheritdoc />
        public Task<IResult> CreateLayer(LayerIdentifier identifier, CancellationToken cancellationToken)
            => throw new System.NotImplementedException();

        /// <inheritdoc />
        public Task<IResult> CreateConfiguration(ConfigurationIdentifier identifier, CancellationToken cancellationToken)
            => throw new System.NotImplementedException();

        /// <inheritdoc />
        public Task<IResult> AssignEnvironmentLayers(
            EnvironmentIdentifier environmentIdentifier,
            IList<LayerIdentifier> layerIdentifiers,
            CancellationToken cancellationToken)
            => throw new System.NotImplementedException();

        /// <inheritdoc />
        public Task<IResult> ModifyLayerKeys(LayerIdentifier identifier, IList<ConfigKeyAction> actions, CancellationToken cancellationToken)
            => throw new System.NotImplementedException();

        /// <inheritdoc />
        public Task<IResult> ModifyLayerKeys(LayerIdentifier identifier, IList<EnvironmentLayerKey> keys, CancellationToken cancellationToken)
            => throw new System.NotImplementedException();

        /// <inheritdoc />
        public Task<IResult> ModifyStructureVariables(StructureIdentifier identifier, IList<ConfigKeyAction> actions, CancellationToken cancellationToken)
            => throw new System.NotImplementedException();
    }
}
