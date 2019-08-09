using System;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;

namespace Bechtle.A365.ConfigService.Common.Utilities
{
    public static class CacheUtilities
    {
        public static string MakeCacheKey(params object[] items) => $"Mav.CS:{string.Join("", items.Select(i => i?.ToString() ?? string.Empty))}";

        public static ICacheEntry SetDuration(this ICacheEntry entry, CacheDuration duration)
        {
            if (entry is null)
                return null;

            switch (duration)
            {
                case CacheDuration.None:
                    return entry.SetDuration(null, null);

                case CacheDuration.Tiny:
                    return entry.SetDuration(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1));

                case CacheDuration.Short:
                    return entry.SetDuration(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(10));

                case CacheDuration.Medium:
                    return entry.SetDuration(TimeSpan.FromSeconds(240), TimeSpan.FromSeconds(60));

                default:
                    return entry.SetDuration(null, null);
            }
        }

        private static ICacheEntry SetDuration(this ICacheEntry entry,
                                               TimeSpan? absoluteRelativeToNow,
                                               TimeSpan? sliding)
        {
            entry.AbsoluteExpirationRelativeToNow = absoluteRelativeToNow;
            entry.SlidingExpiration = sliding;
            return entry;
        }
    }
}