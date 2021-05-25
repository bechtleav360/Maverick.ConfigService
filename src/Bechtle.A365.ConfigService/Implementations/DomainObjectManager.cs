using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Interfaces;

namespace Bechtle.A365.ConfigService.Implementations
{
    /// <summary>
    ///     Default-Implementation of <see cref="IDomainObjectManager"/>
    /// </summary>
    public class DomainObjectManager : IDomainObjectManager
    {
        /// <inheritdoc />
        public Task<Result<ConfigEnvironment>> GetEnvironment(EnvironmentIdentifier identifier, CancellationToken cancellationToken)
            => throw new System.NotImplementedException();

        /// <inheritdoc />
        public Task<Result<ConfigEnvironment>> DeleteEnvironment(EnvironmentIdentifier identifier, CancellationToken cancellationToken)
            => throw new System.NotImplementedException();

        /// <inheritdoc />
        public Task<Result<ConfigStructure>> GetStructure(StructureIdentifier identifier, CancellationToken cancellationToken)
            => throw new System.NotImplementedException();

        /// <inheritdoc />
        public Task<Result<EnvironmentLayer>> GetLayer(LayerIdentifier identifier, CancellationToken cancellationToken)
            => throw new System.NotImplementedException();

        /// <inheritdoc />
        public Task<Result<EnvironmentLayer>> DeleteLayer(LayerIdentifier identifier, CancellationToken cancellationToken)
            => throw new System.NotImplementedException();

        /// <inheritdoc />
        public Task<Result<PreparedConfiguration>> GetConfiguration(ConfigurationIdentifier identifier, CancellationToken cancellationToken)
            => throw new System.NotImplementedException();

        /// <inheritdoc />
        public Task<Result<IDictionary<string, string>>> GetConfigurationKeys(ConfigurationIdentifier identifier, CancellationToken cancellationToken)
            => throw new System.NotImplementedException();

        /// <inheritdoc />
        public Task<Result> CreateEnvironment(EnvironmentIdentifier identifier, CancellationToken cancellationToken)
            => throw new System.NotImplementedException();

        /// <inheritdoc />
        public Task<Result> CreateStructure(
            StructureIdentifier identifier,
            IDictionary<string, string> keys,
            IDictionary<string, string> variables,
            CancellationToken cancellationToken)
            => throw new System.NotImplementedException();

        /// <inheritdoc />
        public Task<Result> CreateLayer(LayerIdentifier identifier, CancellationToken cancellationToken)
            => throw new System.NotImplementedException();

        /// <inheritdoc />
        public Task<Result> CreateConfiguration(ConfigurationIdentifier identifier, CancellationToken cancellationToken)
            => throw new System.NotImplementedException();

        /// <inheritdoc />
        public Task<Result> AssignEnvironmentLayers(
            EnvironmentIdentifier environmentIdentifier,
            IList<LayerIdentifier> layerIdentifiers,
            CancellationToken cancellationToken)
            => throw new System.NotImplementedException();

        /// <inheritdoc />
        public Task<Result> ModifyLayerKeys(LayerIdentifier identifier, IList<ConfigKeyAction> actions, CancellationToken cancellationToken)
            => throw new System.NotImplementedException();

        /// <inheritdoc />
        public Task<Result> ModifyLayerKeys(LayerIdentifier identifier, IList<EnvironmentLayerKey> keys, CancellationToken cancellationToken)
            => throw new System.NotImplementedException();

        /// <inheritdoc />
        public Task<Result> ModifyStructureVariables(StructureIdentifier identifier, IList<ConfigKeyAction> actions, CancellationToken cancellationToken)
            => throw new System.NotImplementedException();
    }
}
