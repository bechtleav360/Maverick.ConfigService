using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Interfaces.Stores;

namespace Bechtle.A365.ConfigService.Implementations.Stores
{
    /// <inheritdoc cref="IProjectionStore" />
    public sealed class ProjectionStore : IProjectionStore
    {
        /// <inheritdoc cref="ProjectionStore" />
        public ProjectionStore(
            ILayerProjectionStore layerStore,
            IStructureProjectionStore structureStore,
            IEnvironmentProjectionStore environmentStore,
            IConfigurationProjectionStore configurationStore)
        {
            Layers = layerStore;
            Structures = structureStore;
            Environments = environmentStore;
            Configurations = configurationStore;
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            await Configurations.DisposeAsync();
            await Environments.DisposeAsync();
            await Structures.DisposeAsync();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Configurations.Dispose();
            Environments.Dispose();
            Structures.Dispose();
        }

        /// <inheritdoc />
        public IConfigurationProjectionStore Configurations { get; }

        /// <inheritdoc />
        public IEnvironmentProjectionStore Environments { get; }

        /// <inheritdoc />
        public ILayerProjectionStore Layers { get; }

        /// <inheritdoc />
        public IStructureProjectionStore Structures { get; }
    }
}
