using System;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;

namespace Bechtle.A365.ConfigService.Common.Utilities
{
    public static class CacheUtilities
    {
        public static ICacheEntry SetDuration(this ICacheEntry entry, CacheDuration duration)
        {
            if (entry is null)
                return null;

            switch (duration)
            {
                case CacheDuration.Tiny:
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5);
                    entry.SlidingExpiration = TimeSpan.FromSeconds(1);
                    break;

                case CacheDuration.Short:
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60);
                    entry.SlidingExpiration = TimeSpan.FromSeconds(10);
                    break;

                case CacheDuration.Medium:
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(240);
                    entry.SlidingExpiration = TimeSpan.FromSeconds(60);
                    break;
            }

            return entry;
        }

        public static string MakeCacheKey(params object[] items) => $"Mav.CS:{string.Join("", items.Select(i => i.ToString()))}";
    }

    /// <summary>
    ///     General length of how long an object should be cached
    /// </summary>
    public enum CacheDuration
    {
        /// <summary>
        ///     item should be cached for a few subsequent requests (<5s)
        /// </summary>
        Tiny,

        /// <summary>
        ///     item should be cached for a few requests that may be up to a minute apart (<1m)
        /// </summary>
        Short,

        /// <summary>
        ///     item should be cached for a several requests across different actions (<4m)
        /// </summary>
        Medium
    }
}