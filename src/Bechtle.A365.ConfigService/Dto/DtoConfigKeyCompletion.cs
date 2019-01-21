namespace Bechtle.A365.ConfigService.Dto
{
    /// <summary>
    ///     completion-data for a searched path
    /// </summary>
    public class DtoConfigKeyCompletion
    {
        /// <summary>
        ///     next part in the searched path
        /// </summary>
        public string Completion { get; set; }

        /// <summary>
        ///     full path including this one
        /// </summary>
        public string FullPath { get; set; }

        /// <summary>
        ///     true if this is part in the path has children
        /// </summary>
        public bool HasChildren { get; set; }
    }
}