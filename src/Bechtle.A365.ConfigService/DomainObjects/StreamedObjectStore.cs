using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Services.Stores;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     default ObjectStore using <see cref="IEventStore" /> and <see cref="ISnapshotStore" /> for retrieving Objects
    /// </summary>
    public class StreamedObjectStore : IStreamedStore
    {
        private readonly IEventStore _eventStore;
        private readonly ILogger<StreamedObjectStore> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly ISnapshotStore _snapshotStore;

        /// <inheritdoc />
        public StreamedObjectStore(IEventStore eventStore,
                                   ISnapshotStore snapshotStore,
                                   IMemoryCache memoryCache,
                                   ILogger<StreamedObjectStore> logger)
        {
            _eventStore = eventStore;
            _snapshotStore = snapshotStore;
            _memoryCache = memoryCache;
            _logger = logger;
        }

        /// <inheritdoc />
        public Task<IResult<T>> GetStreamedObject<T>() where T : StreamedObject, new()
            => GetStreamedObject<T>(long.MaxValue);

        /// <inheritdoc />
        public Task<IResult<T>> GetStreamedObject<T>(long maxVersion) where T : StreamedObject, new()
            => GetStreamedObjectInternal(new T(), typeof(T).Name, maxVersion, typeof(T).Name);

        /// <inheritdoc />
        public Task<IResult<T>> GetStreamedObject<T>(T streamedObject, string identifier) where T : StreamedObject
            => GetStreamedObject(streamedObject, identifier, long.MaxValue);

        /// <inheritdoc />
        public Task<IResult<T>> GetStreamedObject<T>(T streamedObject, string identifier, long maxVersion) where T : StreamedObject
            => GetStreamedObjectInternal(streamedObject, identifier, maxVersion, identifier);

        private async Task<IResult<T>> GetStreamedObjectInternal<T>(T streamedObject,
                                                                    string identifier,
                                                                    long maxVersion,
                                                                    string cacheKey)
            where T : StreamedObject
        {
            try
            {
                if (_memoryCache.TryGetValue(cacheKey, out T cachedInstance))
                    return Result.Success(cachedInstance);

                // @TODO: snapshot should be at or below maxVersion
                var latestSnapshot = await _snapshotStore.GetSnapshot<T>(identifier);

                if (!latestSnapshot.IsError)
                    streamedObject.ApplySnapshot(latestSnapshot.Data);

                await StreamObjectToVersion(streamedObject, maxVersion);

                var size = streamedObject.CalculateCacheSize();
                var priority = streamedObject.GetCacheItemPriority();

                _logger.LogInformation($"item cached: priority={priority}; size={size}; key={cacheKey}");

                var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));

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

        private async Task StreamObjectToVersion(StreamedObject streamedObject, long maxVersion)
        {
            // skip streaming entirely if the object is at or above the desired version
            if (streamedObject.CurrentVersion >= maxVersion)
                return;

            var handledEvents = streamedObject.GetHandledEvents();

            await _eventStore.ReplayEventsAsStream(
                @event => handledEvents.Contains(@event.EventType),
                tuple =>
                {
                    var (recordedEvent, domainEvent) = tuple;

                    // stop at the designated max-version
                    if (recordedEvent.EventNumber > maxVersion)
                        return false;

                    streamedObject.ApplyEvent(new StreamedEvent
                    {
                        UtcTime = recordedEvent.Created.ToUniversalTime(),
                        Version = recordedEvent.EventNumber,
                        DomainEvent = domainEvent
                    });

                    return true;
                }, startIndex: streamedObject.CurrentVersion);
        }
    }
}