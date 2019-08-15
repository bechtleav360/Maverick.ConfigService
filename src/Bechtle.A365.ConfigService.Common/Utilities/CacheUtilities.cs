using System;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace Bechtle.A365.ConfigService.Common.Utilities
{
    public static class CacheUtilities
    {
        private static readonly TimeSpan MinimumAbsoluteTimeSpan = TimeSpan.FromMilliseconds(100);

        public static string MakeCacheKey(params object[] items) => $"Mav.CS:{string.Join("", items.Select(i => i?.ToString() ?? string.Empty))}";

        public static ICacheEntry SetDuration(this ICacheEntry entry, CacheDuration duration, IConfiguration configuration = null)
        {
            if (entry is null)
                return null;

            var useCache = configuration?.GetSection("MemoryCache:Local:Enabled").Get<bool>() ?? false;
            var factor = configuration?.GetSection("MemoryCache:Local:Factor").Get<double>() ?? 0;

            if (!useCache)
                return entry.SetDuration(null, null, factor);

            switch (duration)
            {
                case CacheDuration.None:
                    return entry.SetDuration(null, null, factor);

                case CacheDuration.Tiny:
                    return entry.SetDuration(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1), factor);

                case CacheDuration.Short:
                    return entry.SetDuration(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(10), factor);

                case CacheDuration.Medium:
                    return entry.SetDuration(TimeSpan.FromSeconds(240), TimeSpan.FromSeconds(60), factor);

                default:
                    return entry.SetDuration(null, null, factor);
            }
        }

        private static ICacheEntry SetDuration(this ICacheEntry entry,
                                               TimeSpan? absoluteRelativeToNow,
                                               TimeSpan? sliding,
                                               double factor)
        {
            if (factor < 0)
            {
                entry.AbsoluteExpirationRelativeToNow = MinimumAbsoluteTimeSpan;
                return entry;
            }

            absoluteRelativeToNow *= factor;
            sliding *= factor;

            entry.AbsoluteExpirationRelativeToNow = absoluteRelativeToNow < MinimumAbsoluteTimeSpan
                                                        ? MinimumAbsoluteTimeSpan
                                                        : absoluteRelativeToNow ?? MinimumAbsoluteTimeSpan;
            entry.SlidingExpiration = sliding < MinimumAbsoluteTimeSpan
                                          ? MinimumAbsoluteTimeSpan
                                          : sliding ?? MinimumAbsoluteTimeSpan;

            return entry;
        }
    }
}