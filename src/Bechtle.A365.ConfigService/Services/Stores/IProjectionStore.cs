namespace Bechtle.A365.ConfigService.Services.Stores
{
    /// <summary>
    ///     read projected configurations
    /// </summary>
    public interface IProjectionStore
    {
        /// <inheritdoc cref="IConfigurationProjectionStore" />
        IConfigurationProjectionStore Configurations { get; }

        /// <inheritdoc cref="IEnvironmentProjectionStore" />
        IEnvironmentProjectionStore Environments { get; }

        /// <inheritdoc cref="IStructureProjectionStore" />
        IStructureProjectionStore Structures { get; }
    }
}