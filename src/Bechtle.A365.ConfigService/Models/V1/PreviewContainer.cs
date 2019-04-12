namespace Bechtle.A365.ConfigService.Models.V1
{
    /// <summary>
    ///     Container for data that should be previewed
    /// </summary>
    public class PreviewContainer
    {
        /// <inheritdoc cref="EnvironmentPreview" />
        public EnvironmentPreview Environment { get; set; }

        /// <inheritdoc cref="StructurePreview" />
        public StructurePreview Structure { get; set; }
    }
}