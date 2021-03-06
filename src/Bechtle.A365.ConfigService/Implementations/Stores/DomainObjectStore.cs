﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Bechtle.A365.ConfigService.Implementations.Stores
{
    /// <summary>
    ///     default ObjectStore using <see cref="IEventStore" /> and <see cref="ISnapshotStore" /> for retrieving Objects
    /// </summary>
    public sealed class DomainObjectStore : IDomainObjectStore
    {
        private readonly IConfiguration _configuration;
        private readonly TimeSpan _defaultTimeSpan = TimeSpan.FromMinutes(15);
        private readonly IEventStore _eventStore;
        private readonly ILogger<DomainObjectStore> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly ISnapshotStore _snapshotStore;

        /// <inheritdoc cref="DomainObjectStore" />
        public DomainObjectStore(IEventStore eventStore,
                                 ISnapshotStore snapshotStore,
                                 IMemoryCache memoryCache,
                                 IConfiguration configuration,
                                 ILogger<DomainObjectStore> logger)
        {
            _eventStore = eventStore;
            _snapshotStore = snapshotStore;
            _memoryCache = memoryCache;
            _configuration = configuration;
            _logger = logger;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _snapshotStore?.Dispose();
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (_snapshotStore != null)
                await _snapshotStore.DisposeAsync();
        }

        /// <inheritdoc />
        public Task<IResult<T>> ReplayObject<T>() where T : DomainObject, new()
            => ReplayObject<T>(long.MaxValue);

        /// <inheritdoc />
        public Task<IResult<T>> ReplayObject<T>(long maxVersion) where T : DomainObject, new()
            => ReplayObjectInternal(new T(), typeof(T).Name, maxVersion, typeof(T).Name);

        /// <inheritdoc />
        public Task<IResult<T>> ReplayObject<T>(T domainObject, string identifier) where T : DomainObject
            => ReplayObject(domainObject, identifier, long.MaxValue);

        /// <inheritdoc />
        public Task<IResult<T>> ReplayObject<T>(T domainObject, string identifier, long maxVersion) where T : DomainObject
            => ReplayObjectInternal(domainObject, identifier, maxVersion, identifier);

        private async Task ApplyLatestSnapshot<T>(T domainObject, string identifier, long maxVersion, string cacheKey)
            where T : DomainObject
        {
            _logger.LogDebug($"no cached item for key '{cacheKey}' found, attempting to " +
                             $"retrieve latest snapshot below or at version {maxVersion} for '{identifier}'");

            var latestSnapshot = await _snapshotStore.GetSnapshot<T>(identifier, maxVersion);

            if (latestSnapshot.IsError)
                return;

            _logger.LogDebug($"snapshot for '{identifier}' found, " +
                             $"version '{latestSnapshot.Data.Version}', " +
                             $"meta '{latestSnapshot.Data.MetaVersion}'");

            domainObject.ApplySnapshot(latestSnapshot.Data);
        }

        private TimeSpan GetCacheTime()
        {
            try
            {
                var span = _configuration.GetSection("MemoryCache:Local:Duration").Get<TimeSpan?>();

                if (span is null || span.Value <= TimeSpan.Zero)
                    return _defaultTimeSpan;

                return span.Value;
            }
            catch (Exception)
            {
                return _defaultTimeSpan;
            }
        }

        private async Task<IResult<T>> ReplayObjectInternal<T>(T domainObject,
                                                               string identifier,
                                                               long maxVersion,
                                                               string cacheKey)
            where T : DomainObject
        {
            try
            {
                if (TryGetCachedItem(cacheKey, maxVersion, out T cachedInstance))
                    domainObject = cachedInstance;
                else
                    await ApplyLatestSnapshot(domainObject, identifier, maxVersion, cacheKey);

                _logger.LogDebug($"replaying events for '{identifier}', '{domainObject.MetaVersion}' => '{maxVersion}'");

                // if the target-object is in any way streamed we save a snapshot of it
                if ((await StreamObjectToVersion(domainObject, maxVersion, identifier)).Any())
                    IncrementalSnapshotService.QueueSnapshot(domainObject.CreateSnapshot());

                var size = domainObject.CalculateCacheSize();
                var priority = domainObject.GetCacheItemPriority();
                var cacheDuration = GetCacheTime();

                _logger.LogInformation($"item cached: duration={cacheDuration:g}; priority={priority}; size={size}; key={cacheKey}");

                var cts = new CancellationTokenSource(cacheDuration);
                (CancellationTokenSource, ILogger<DomainObjectStore>) callbackParams = (cts, _logger);

                _memoryCache.Set(cacheKey,
                                 domainObject,
                                 new MemoryCacheEntryOptions()
                                     .SetPriority(priority)
                                     .SetSize(size)
                                     .AddExpirationToken(new CancellationChangeToken(cts.Token))
                                     .RegisterPostEvictionCallback((key, value, reason, state) =>
                                     {
                                         var (tokenSource, logger) = ((CancellationTokenSource, ILogger<DomainObjectStore>)) state;
                                         tokenSource?.Dispose();
                                         logger?.LogInformation($"cached item '{key}' evicted: {reason}");
                                     }, callbackParams));

                return Result.Success(domainObject);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"failed to retrieve {typeof(T).Name} from EventStore (" +
                                      $"{nameof(identifier)}: {identifier}, " +
                                      $"{nameof(maxVersion)}: {maxVersion}, " +
                                      $"{nameof(cacheKey)}: {cacheKey})");
                return Result.Error<T>($"failed to retrieve {typeof(T).Name} from EventStore", ErrorCode.FailedToRetrieveItem);
            }
        }

        private async Task<List<long>> StreamObjectToVersion(DomainObject domainObject, long maxVersion, string identifier)
        {
            // skip streaming entirely if the object is at or above the desired version
            if (domainObject.MetaVersion >= maxVersion)
                return new List<long>();

            var appliedEvents = new List<long>();

            await _eventStore.ReplayEventsAsStream(
                tuple =>
                {
                    var (recordedEvent, domainEvent) = tuple;

                    // stop at the designated max-version
                    if (recordedEvent.EventNumber > maxVersion)
                        return false;

                    var versionBefore = domainObject.CurrentVersion;

                    domainObject.ApplyEvent(new ReplayedEvent
                    {
                        UtcTime = recordedEvent.UtcTime.ToUniversalTime(),
                        Version = recordedEvent.EventNumber,
                        DomainEvent = domainEvent
                    });

                    if (domainObject.CurrentVersion > versionBefore)
                    {
                        _logger.LogTrace($"applied event {recordedEvent.EventNumber} / '{recordedEvent.EventType}' to '{identifier}'");
                        appliedEvents.Add(recordedEvent.EventNumber);
                    }

                    return true;
                }, startIndex: domainObject.MetaVersion);

            _logger.LogDebug($"applied {appliedEvents.Count} events to '{identifier}': {string.Join(", ", appliedEvents)}");
            return appliedEvents;
        }

        private bool TryGetCachedItem<T>(string cacheKey, long maxVersion, out T item)
            where T : DomainObject
        {
            if (!_memoryCache.TryGetValue(cacheKey, out item))
            {
                item = null;
                return false;
            }

            _logger.LogDebug($"retrieved cached item for key '{cacheKey}'");

            if (item.CurrentVersion <= maxVersion)
                return true;

            _logger.LogDebug($"cached item for key '{cacheKey}' is beyond maximum allowed version {item.CurrentVersion} > {maxVersion}");
            return false;
        }
    }
}