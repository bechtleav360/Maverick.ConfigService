namespace Bechtle.A365.ConfigService.Common.Objects
{
    /// <summary>
    ///     completion-data for a searched path
    /// </summary>
    public class DtoConfigKeyCompletion
    {
        /// <summary>
        ///     next part in the searched path
        /// </summary>
        public string Completion { get; set; } = string.Empty;

        /// <summary>
        ///     full path including this one
        /// </summary>
        public string FullPath { get; set; } = string.Empty;

        /// <summary>
        ///     true if this is part in the path has children
        /// </summary>
        public bool HasChildren { get; set; }
    }
}
