namespace Bechtle.A365.ConfigService.Services
{
    /// <summary>
    ///     read projected configurations
    /// </summary>
    public interface IProjectionStore
    {
        /// <inheritdoc cref="IStructureProjectionStore"/>
        IStructureProjectionStore Structures { get; }

        /// <inheritdoc cref="IEnvironmentProjectionStore"/>
        IEnvironmentProjectionStore Environments { get; }

        /// <inheritdoc cref="IConfigurationProjectionStore"/>
        IConfigurationProjectionStore Configurations { get; }
    }
}