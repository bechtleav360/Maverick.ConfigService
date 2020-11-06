namespace Bechtle.A365.ConfigService.Common.Objects
{
    /// <summary>
    ///     collection of exported parts of the configuration
    /// </summary>
    public class ConfigExport
    {
        /// <inheritdoc cref="EnvironmentExport"/>
        public EnvironmentExport[] Environments { get; set; }

        /// <inheritdoc cref="LayerExport"/>
        public LayerExport[] Layers { get; set; }
    }
}