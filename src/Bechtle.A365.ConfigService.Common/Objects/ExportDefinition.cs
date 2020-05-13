namespace Bechtle.A365.ConfigService.Common.Objects
{
    /// <summary>
    ///     definition of what should be exported
    /// </summary>
    public class ExportDefinition
    {
        /// <summary>
        ///     list of environments that should be exported
        /// </summary>
        public EnvironmentExportDefinition[] Environments { get; set; }
    }
}