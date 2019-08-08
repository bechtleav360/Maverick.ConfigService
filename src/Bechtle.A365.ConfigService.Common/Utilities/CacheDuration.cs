namespace Bechtle.A365.ConfigService.Common.Utilities
{
    /// <summary>
    ///     General length of how long an object should be cached
    /// </summary>
    public enum CacheDuration
    {
        /// <summary>
        ///     item should be cached for a few subsequent requests (up to 5s)
        /// </summary>
        Tiny,

        /// <summary>
        ///     item should be cached for a few requests that may be up to a minute apart (up to 1m)
        /// </summary>
        Short,

        /// <summary>
        ///     item should be cached for a several requests across different actions (up to 4m)
        /// </summary>
        Medium
    }
}