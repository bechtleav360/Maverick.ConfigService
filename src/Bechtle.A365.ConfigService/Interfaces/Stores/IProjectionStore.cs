using System;

namespace Bechtle.A365.ConfigService.Interfaces.Stores
{
    /// <summary>
    ///     read projected configurations
    /// </summary>
    public interface IProjectionStore : IDisposable, IAsyncDisposable
    {
        /// <inheritdoc cref="IConfigurationProjectionStore" />
        IConfigurationProjectionStore Configurations { get; }

        /// <inheritdoc cref="IEnvironmentProjectionStore" />
        IEnvironmentProjectionStore Environments { get; }

        /// <inheritdoc cref="ILayerProjectionStore" />
        ILayerProjectionStore Layers { get; }

        /// <inheritdoc cref="IStructureProjectionStore" />
        IStructureProjectionStore Structures { get; }
    }
}