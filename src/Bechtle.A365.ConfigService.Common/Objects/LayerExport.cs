namespace Bechtle.A365.ConfigService.Common.Objects
{
    /// <summary>
    ///     a single Layer, exported for later import
    /// </summary>
    public class LayerExport
    {
        /// <summary>
        ///     Name of the exported Layer
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     List of Keys currently contained in this Layer
        /// </summary>
        public EnvironmentKeyExport[] Keys { get; set; }
    }
}