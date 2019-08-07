namespace Bechtle.A365.ConfigService.Models.V1
{
    /// <summary>
    ///     stats about the compilation (ref-counts, hits / misses, etc)
    /// </summary>
    public class CompilationStats
    {
        /// <summary>
        ///     number of used References
        /// </summary>
        public int ReferencesUsed { get; set; }

        /// <summary>
        ///     number of used Static values
        /// </summary>
        public int StaticValuesUsed { get; set; }
    }
}