using Microsoft.EntityFrameworkCore;

namespace Bechtle.A365.ConfigService.Services.Stores
{
    /// <inheritdoc cref="DbContext" />
    /// <inheritdoc cref="IProjectionStore" />
    public class ProjectionStore : DbContext, IProjectionStore
    {
        /// <inheritdoc />
        /// <param name="metadataStore"></param>
        /// <param name="structureStore"></param>
        /// <param name="environmentStore"></param>
        /// <param name="configurationStore"></param>
        public ProjectionStore(IMetadataProjectionStore metadataStore,
                               IStructureProjectionStore structureStore,
                               IEnvironmentProjectionStore environmentStore,
                               IConfigurationProjectionStore configurationStore)
        {
            Metadata = metadataStore;
            Structures = structureStore;
            Environments = environmentStore;
            Configurations = configurationStore;
        }

        /// <inheritdoc />
        public IConfigurationProjectionStore Configurations { get; }

        /// <inheritdoc />
        public IEnvironmentProjectionStore Environments { get; }

        /// <inheritdoc />
        public IStructureProjectionStore Structures { get; }

        /// <inheritdoc />
        public IMetadataProjectionStore Metadata { get; }
    }
}