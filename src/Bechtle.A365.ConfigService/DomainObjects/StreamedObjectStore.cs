using System;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Services;
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
        public Task<IResult<StreamedConfiguration>> GetConfiguration(ConfigurationIdentifier identifier)
            => GetConfiguration(identifier, long.MaxValue);

        /// <inheritdoc />
        public Task<IResult<StreamedConfiguration>> GetConfiguration(ConfigurationIdentifier identifier, long maxVersion)
            => GetStreamedObjectInternal(new StreamedConfiguration(identifier), maxVersion, identifier.ToString());

        /// <inheritdoc />
        public Task<IResult<StreamedConfigurationList>> GetConfigurationList()
            => GetConfigurationList(long.MaxValue);

        /// <inheritdoc />
        public Task<IResult<StreamedConfigurationList>> GetConfigurationList(long maxVersion)
            => GetStreamedObjectInternal(new StreamedConfigurationList(), maxVersion, nameof(StreamedConfigurationList));

        /// <inheritdoc />
        public Task<IResult<StreamedEnvironment>> GetEnvironment(EnvironmentIdentifier identifier)
            => GetEnvironment(identifier, long.MaxValue);

        /// <inheritdoc />
        public Task<IResult<StreamedEnvironment>> GetEnvironment(EnvironmentIdentifier identifier, long maxVersion)
            => GetStreamedObjectInternal(new StreamedEnvironment(identifier), maxVersion, identifier.ToString());

        /// <inheritdoc />
        public Task<IResult<StreamedEnvironmentList>> GetEnvironmentList()
            => GetEnvironmentList(long.MaxValue);

        /// <inheritdoc />
        public Task<IResult<StreamedEnvironmentList>> GetEnvironmentList(long maxVersion)
            => GetStreamedObjectInternal(new StreamedEnvironmentList(), maxVersion, nameof(StreamedEnvironmentList));

        /// <inheritdoc />
        public Task<IResult<StreamedStructure>> GetStructure(StructureIdentifier identifier)
            => GetStructure(identifier, long.MaxValue);

        /// <inheritdoc />
        public Task<IResult<StreamedStructure>> GetStructure(StructureIdentifier identifier, long maxVersion)
            => GetStreamedObjectInternal(new StreamedStructure(identifier), maxVersion, identifier.ToString());

        /// <inheritdoc />
        public Task<IResult<StreamedStructureList>> GetStructureList()
            => GetStructureList(long.MaxValue);

        /// <inheritdoc />
        public Task<IResult<StreamedStructureList>> GetStructureList(long maxVersion)
            => GetStreamedObjectInternal(new StreamedStructureList(), maxVersion, nameof(StreamedStructureList));

        private async Task<IResult<T>> GetStreamedObjectInternal<T>(T streamedObject, long maxVersion, string cacheKey) where T : StreamedObject
        {
            try
            {
                if (_memoryCache.TryGetValue(cacheKey, out T cachedInstance))
                    return Result.Success(cachedInstance);

                var latestSnapshot = await _snapshotStore.GetStructureList();

                if (!latestSnapshot.IsError)
                    streamedObject.ApplySnapshot(latestSnapshot.Data);

                await StreamObjectToVersion(streamedObject, maxVersion);

                var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

                var size = streamedObject.CalculateCacheSize();
                var priority = streamedObject.GetCacheItemPriority();

                _logger.LogInformation($"item cached: priority={priority}; size={size}; key={cacheKey}");

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
            // @TODO: start streaming from latestSnapshot.Version
            await _eventStore.ReplayEventsAsStream(tuple =>
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