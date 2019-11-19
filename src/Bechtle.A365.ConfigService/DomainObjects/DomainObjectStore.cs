using System;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     default ObjectStore using <see cref="IEventStore" /> and <see cref="ISnapshotStore" /> for retrieving Objects
    /// </summary>
    public class DomainObjectStore : IStreamedStore
    {
        private readonly TimeSpan _defaultTimeSpan = TimeSpan.FromMinutes(15);
        private readonly IConfiguration _configuration;
        private readonly IEventStore _eventStore;
        private readonly ILogger<DomainObjectStore> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly ISnapshotStore _snapshotStore;

        /// <inheritdoc />
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
        public Task<IResult<T>> GetStreamedObject<T>() where T : DomainObject, new()
            => GetStreamedObject<T>(long.MaxValue);

        /// <inheritdoc />
        public Task<IResult<T>> GetStreamedObject<T>(long maxVersion) where T : DomainObject, new()
            => GetStreamedObjectInternal(new T(), typeof(T).Name, maxVersion, typeof(T).Name, false);

        /// <inheritdoc />
        public Task<IResult<T>> GetStreamedObject<T>(T streamedObject, string identifier) where T : DomainObject
            => GetStreamedObject(streamedObject, identifier, long.MaxValue);

        /// <inheritdoc />
        public Task<IResult<T>> GetStreamedObject<T>(T streamedObject, string identifier, long maxVersion) where T : DomainObject
            => GetStreamedObjectInternal(streamedObject, identifier, maxVersion, identifier, true);

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

        private async Task<IResult<T>> GetStreamedObjectInternal<T>(T streamedObject,
                                                                    string identifier,
                                                                    long maxVersion,
                                                                    string cacheKey,
                                                                    bool useMetadataFilter)
            where T : DomainObject
        {
            try
            {
                if (_memoryCache.TryGetValue(cacheKey, out T cachedInstance))
                    return Result.Success(cachedInstance);

                var latestSnapshot = await _snapshotStore.GetSnapshot<T>(identifier, maxVersion);

                if (!latestSnapshot.IsError)
                    streamedObject.ApplySnapshot(latestSnapshot.Data);

                await StreamObjectToVersion(streamedObject, maxVersion, identifier, useMetadataFilter);

                var size = streamedObject.CalculateCacheSize();
                var priority = streamedObject.GetCacheItemPriority();

                _logger.LogInformation($"item cached: priority={priority}; size={size}; key={cacheKey}");

                var cts = new CancellationTokenSource(GetCacheTime());

                _memoryCache.Set(cacheKey,
                                 streamedObject,
                                 new MemoryCacheEntryOptions()
                                     .SetPriority(priority)
                                     .SetSize(size)
                                     .AddExpirationToken(new CancellationChangeToken(cts.Token))
                                     .RegisterPostEvictionCallback((key, value, reason, state) =>
                                     {
                                         cts.Dispose();
                                         _logger.LogInformation($"item '{key}' evicted: {reason}");
                                     }));

                return Result.Success(streamedObject);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"failed to retrieve {typeof(T).Name} from EventStore");
                return Result.Error<T>($"failed to retrieve {typeof(T).Name} from EventStore", ErrorCode.FailedToRetrieveItem);
            }
        }

        private async Task StreamObjectToVersion(DomainObject domainObject, long maxVersion, string identifier, bool useMetadataFilter)
        {
            // skip streaming entirely if the object is at or above the desired version
            if (domainObject.CurrentVersion >= maxVersion)
                return;

            var handledEvents = domainObject.GetHandledEvents();

            await _eventStore.ReplayEventsAsStream(
                tuple =>
                {
                    var (recordedEvent, metadata) = tuple;

                    if (useMetadataFilter && metadata[KnownDomainEventMetadata.Identifier].Equals(identifier))
                        return true;

                    return handledEvents.Contains(recordedEvent.EventType);
                },
                tuple =>
                {
                    var (recordedEvent, domainEvent) = tuple;

                    // stop at the designated max-version
                    if (recordedEvent.EventNumber > maxVersion)
                        return false;

                    domainObject.ApplyEvent(new StreamedEvent
                    {
                        UtcTime = recordedEvent.Created.ToUniversalTime(),
                        Version = recordedEvent.EventNumber,
                        DomainEvent = domainEvent
                    });

                    return true;
                }, startIndex: domainObject.CurrentVersion);
        }
    }
}