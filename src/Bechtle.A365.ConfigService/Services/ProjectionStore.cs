using Microsoft.EntityFrameworkCore;

namespace Bechtle.A365.ConfigService.Services
{
    /// <inheritdoc cref="DbContext" />
    /// <inheritdoc cref="IProjectionStore" />
    public class ProjectionStore : DbContext, IProjectionStore
    {
        /// <summary>
        /// </summary>
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
        public IStructureProjectionStore Structures { get; }

        /// <inheritdoc />
        public IEnvironmentProjectionStore Environments { get; }

        /// <inheritdoc />
        public IConfigurationProjectionStore Configurations { get; }
    }
}