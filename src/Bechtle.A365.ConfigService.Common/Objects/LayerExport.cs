namespace Bechtle.A365.ConfigService.Common.Objects
{
    /// <summary>
    ///     a single Layer, exported for later import
    /// </summary>
    public class LayerExport
    {
        /// <summary>
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// </summary>
        public EnvironmentKeyExport[] Keys { get; set; }
    }
}