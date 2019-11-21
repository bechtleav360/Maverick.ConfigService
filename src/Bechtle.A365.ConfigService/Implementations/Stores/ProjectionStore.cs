using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Interfaces.Stores;

namespace Bechtle.A365.ConfigService.Implementations.Stores
{
    /// <inheritdoc cref="IProjectionStore" />
    public class ProjectionStore : IProjectionStore
    {
        /// <inheritdoc />
        /// <param name="structureStore"></param>
        /// <param name="environmentStore"></param>
        /// <param name="configurationStore"></param>
        public ProjectionStore(IStructureProjectionStore structureStore,
                               IEnvironmentProjectionStore environmentStore,
                               IConfigurationProjectionStore configurationStore)
        {
            Structures = structureStore;
            Environments = environmentStore;
            Configurations = configurationStore;
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (Configurations != null)
                await Configurations.DisposeAsync();

            if (Environments != null)
                await Environments.DisposeAsync();

            if (Structures != null)
                await Structures.DisposeAsync();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Configurations?.Dispose();
            Environments?.Dispose();
            Structures?.Dispose();
        }

        /// <inheritdoc />
        public IConfigurationProjectionStore Configurations { get; }

        /// <inheritdoc />
        public IEnvironmentProjectionStore Environments { get; }

        /// <inheritdoc />
        public IStructureProjectionStore Structures { get; }
    }
}