using System;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Projection.Services
{
    /// <inheritdoc />
    public class MemoryCacheEventLock : IEventLock
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger _logger;

        private static readonly object CachedEventLock = new object();

        /// <inheritdoc />
        public MemoryCacheEventLock(ILogger<MemoryCacheEventLock> logger,
                                           IDistributedCache cache)
        {
            _logger = logger;
            _cache = cache;
        }

        /// <inheritdoc />
        public Guid TryLockEvent(string eventId, string nodeId, TimeSpan duration)
        {
            var key = $"{eventId}@{nodeId}";

            _logger.LogInformation($"trying to claim lock for '{key}'");

            lock (CachedEventLock)
            {
                byte[] value;

                try
                {
                    value = _cache.Get(key);
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, $"could not read value for lock '{key}'");
                    return Guid.Empty;
                }

                try
                {
                    if (value is null || !value.Any())
                    {
                        var lockId = Guid.NewGuid();
                        _cache.Set(key, lockId.ToByteArray(), new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = duration
                        });
                        return lockId;
                    }

                    _logger.LogInformation($"lock '{key}' has already been claimed");
                    return Guid.Empty;
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, $"could not create new lock for '{key}'");
                    return Guid.Empty;
                }
            }
        }

        /// <inheritdoc />
        public bool TryUnlockEvent(string eventId, string nodeId, Guid owningLockId)
        {
            var key = $"{eventId}@{nodeId}";

            _logger.LogInformation($"trying to free lock for '{key}'");

            lock (CachedEventLock)
            {
                byte[] value;

                try
                {
                    value = _cache.Get(key);
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, $"could not read value for lock '{key}'");
                    return false;
                }

                try
                {
                    if (value is null || !value.Any())
                    {
                        _logger.LogWarning($"{nameof(TryUnlockEvent)} called on non-existing lock; if nothing crashed yet, it will in the future");
                        return true;
                    }

                    var existingLock = new Guid(value);
                    
                    if (existingLock.Equals(owningLockId))
                    {
                        _cache.Remove(key);
                        return true;
                    }

                    _logger.LogWarning($"{nameof(TryUnlockEvent)} called with wrong credentials - can't unlock event; " +
                                       "indicates corrupt logic - if nothing crashed yet, it will in the future");
                    return false;
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, $"could not remove lock for '{key}'");
                    return false;
                }
            }
        }
    }
}