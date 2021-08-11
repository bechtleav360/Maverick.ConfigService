using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Configuration;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Bechtle.A365.ConfigService.Models.V1;
using Bechtle.A365.ServiceBase.Extensions;
using LiteDB;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bechtle.A365.ConfigService.Implementations.Stores
{
    /// <summary>
    ///     default ObjectStore using <see cref="ILiteDatabase" /> for storing objects
    /// </summary>
    public sealed class DomainObjectStore : IDomainObjectStore
    {
        private readonly ILiteDatabase _database;
        private readonly IDomainObjectFileStore _fileStore;
        private readonly ILogger<DomainObjectStore> _logger;
        private readonly IOptions<HistoryConfiguration> _historyConfiguration;
        private readonly IMemoryCache _memoryCache;

        /// <inheritdoc cref="DomainObjectStore" />
        public DomainObjectStore(
            ILogger<DomainObjectStore> logger,
            IOptions<HistoryConfiguration> historyConfiguration,
            IDomainObjectStoreLocationProvider locationProvider,
            IMemoryCache memoryCache,
            IDomainObjectFileStore fileStore)
        {
            // as the name suggests liteDb will convert all empty strings to null, because that's such a good idea!
            // kinda stupid to set this globally (as opposed to per-object for more control),
            // but i don't want to be responsible for other stuff they might do to new objects that we're unaware of
            BsonMapper.Global.EmptyStringToNull = false;

            // this is 'necessary' to allow mapping of longer KeyPath-chains
            // the default of 20 doesn't allow for some path we're creating
            //
            // we take responsibility to not insert crap into the local projection-db, so we can set this limit so high
            BsonMapper.Global.MaxDepth = 1000;

            BsonMapper.Global.EnumAsInteger = true;

            _logger = logger;
            _historyConfiguration = historyConfiguration;
            _memoryCache = memoryCache;
            _fileStore = fileStore;
            _database = new LiteDatabase(
                new ConnectionString
                {
                    Connection = ConnectionType.Shared,
                    Filename = locationProvider.FileName
                });
        }

        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            Dispose();
            return new ValueTask(Task.CompletedTask);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _database?.Dispose();
        }

        /// <inheritdoc />
        public Task<IResult<long>> GetProjectedVersion()
        {
            try
            {
                ILiteCollection<StorageMetadata> collection = _database.GetCollection<StorageMetadata>();
                StorageMetadata lastEntry = collection.Query()
                                                      .OrderByDescending(e => e.CreatedAt)
                                                      .FirstOrDefault();

                if (lastEntry is null)
                {
                    _logger.LogInformation("no record of any projected events found, returning -1 as marker-value");
                    return Task.FromResult(Result.Success<long>(-1));
                }

                return Task.FromResult(Result.Success(lastEntry.LastWrittenEvent));
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "unable to read latest projected event");
                return Task.FromResult(Result.Error<long>("unable to read latest projected event", ErrorCode.DbQueryError));
            }
        }

        /// <inheritdoc />
        public async Task<IResult<Page<TIdentifier>>> ListAll<TObject, TIdentifier>(QueryRange range)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
            => await ListAll<TObject, TIdentifier>(_ => true, range);

        /// <inheritdoc />
        public async Task<IResult<Page<TIdentifier>>> ListAll<TObject, TIdentifier>(Func<TIdentifier, bool> filter, QueryRange range)
            where TObject : DomainObject<TIdentifier> where TIdentifier : Identifier
        {
            try
            {
                List<ObjectLookup<TIdentifier>> objectInfos = await GetObjectInfo<TObject, TIdentifier>();

                IList<TIdentifier> totalItems = objectInfos.OrderBy(o => o.Id.ToString())
                                                           // only list items whose newest version wasn't deleted
                                                           .Where(
                                                               o => !o.Versions
                                                                      .OrderByDescending(i => i.Key)
                                                                      .First()
                                                                      .Value
                                                                      .IsMarkedDeleted)
                                                           .Select(o => o.Id)
                                                           .Where(filter)
                                                           .ToList();
                IList<TIdentifier> ids = totalItems.Where(filter)
                                                   .Skip(range.Offset)
                                                   .Take(range.Length)
                                                   .ToList();

                var page = new Page<TIdentifier>
                {
                    Items = ids,
                    Count = ids.Count,
                    Offset = range.Offset,
                    TotalCount = totalItems.Count
                };

                return Result.Success(page);
            }
            catch (Exception e)
            {
                _logger.LogWarning(
                    e,
                    "unable to list objects of type {DomainObjectType}",
                    typeof(TObject).GetFriendlyName());
                return Result.Error<Page<TIdentifier>>(
                    $"unable to list objects in collection '{typeof(TObject).GetFriendlyName()}'",
                    ErrorCode.DbUpdateError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<Page<TIdentifier>>> ListAll<TObject, TIdentifier>(long version, QueryRange range)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
            => await ListAll<TObject, TIdentifier>(version, _ => true, range);

        /// <inheritdoc />
        public async Task<IResult<Page<TIdentifier>>> ListAll<TObject, TIdentifier>(long version, Func<TIdentifier, bool> filter, QueryRange range)
            where TObject : DomainObject<TIdentifier> where TIdentifier : Identifier
        {
            try
            {
                List<ObjectLookup<TIdentifier>> objectInfos = await GetObjectInfo<TObject, TIdentifier>();

                IList<TIdentifier> totalItems = objectInfos.OrderBy(o => o.Id.ToString())
                                                           // only list items whose newest version wasn't deleted
                                                           .Where(
                                                               o => !o.Versions
                                                                      .OrderByDescending(i => i.Key)
                                                                      .First(i => version < 0 || i.Key <= version)
                                                                      .Value
                                                                      .IsMarkedDeleted)
                                                           .Select(o => o.Id)
                                                           .Where(filter)
                                                           .ToList();
                IList<TIdentifier> ids = totalItems.Skip(range.Offset)
                                                   .Take(range.Length)
                                                   .ToList();

                var page = new Page<TIdentifier>
                {
                    Items = ids,
                    Count = ids.Count,
                    Offset = range.Offset,
                    TotalCount = totalItems.Count
                };

                return Result.Success(page);
            }
            catch (Exception e)
            {
                _logger.LogWarning(
                    e,
                    "unable to list objects of type {DomainObjectType}",
                    typeof(TObject).GetFriendlyName());
                return Result.Error<Page<TIdentifier>>(
                    $"unable to list objects in collection '{typeof(TObject).GetFriendlyName()}'",
                    ErrorCode.DbUpdateError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<TObject>> Load<TObject, TIdentifier>(TIdentifier identifier)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
        {
            if (_memoryCache.TryGetValue(identifier.ToString(), out TObject cachedValue))
            {
                return Result.Success(cachedValue);
            }

            try
            {
                ObjectLookup<TIdentifier> @object = await GetObjectInfo<TObject, TIdentifier>(identifier);
                List<long> possibleVersions = @object.Versions
                                                     .Where(kvp => !kvp.Value.IsMarkedDeleted)
                                                     .OrderByDescending(kvp => kvp.Key)
                                                     .Select(kvp => kvp.Key)
                                                     .ToList();
                long maxExistingVersion = possibleVersions.Any()
                                              ? possibleVersions.FirstOrDefault()
                                              : -1;

                if (maxExistingVersion >= 0)
                {
                    IResult<TObject> result = await _fileStore.LoadObject<TObject, TIdentifier>(identifier, maxExistingVersion);
                    if (result.IsError)
                    {
                        return result;
                    }

                    _memoryCache.Set(identifier.ToString(), result.Data, TimeSpan.FromSeconds(30));
                    return result;
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "unable to load domainObject");
                return Result.Error<TObject>("unable to load domainObject", ErrorCode.DbQueryError);
            }

            _logger.LogWarning(
                "no domainObject with id '{Identifier}' found",
                identifier.ToString());
            return Result.Error<TObject>("unable to load latest version of domainObject", ErrorCode.NotFound);
        }

        /// <inheritdoc />
        public async Task<IResult<TObject>> Load<TObject, TIdentifier>(TIdentifier identifier, long maxVersion)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
        {
            try
            {
                ObjectLookup<TIdentifier> @object = await GetObjectInfo<TObject, TIdentifier>(identifier);

                KeyValuePair<long, ObjectLookupInfo> matchingVersion = @object.Versions
                                                                              .OrderByDescending(kvp => kvp.Key)
                                                                              .FirstOrDefault(
                                                                                  // return first entry when version < 0
                                                                                  kvp => maxVersion < 0
                                                                                         || kvp.Key <= maxVersion
                                                                                         && !kvp.Value.IsMarkedDeleted);

                if (matchingVersion.Value is null)
                {
                    return Result.Error<TObject>("no matching version found", ErrorCode.NotFound);
                }

                if (!matchingVersion.Value.IsDataAvailable)
                {
                    return Result.Error<TObject>("data for this version not retained", ErrorCode.NotFound);
                }

                if (matchingVersion.Key >= 0)
                {
                    return await _fileStore.LoadObject<TObject, TIdentifier>(identifier, matchingVersion.Key);
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "unable to load domainObject at version {Version}", maxVersion);
                return Result.Error<TObject>($"unable to load domainObject at version {maxVersion}", ErrorCode.DbQueryError);
            }

            _logger.LogWarning(
                "no domainObject with id '{Identifier}' at or below version {Version} found",
                identifier.ToString(),
                maxVersion);
            return Result.Error<TObject>($"unable to load domainObject at or below version {maxVersion}", ErrorCode.NotFound);
        }

        /// <inheritdoc />
        public Task<IResult<IDictionary<string, string>>> LoadMetadata<TObject, TIdentifier>(TIdentifier identifier)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
        {
            string collectionName = typeof(TObject).Name + "_Metadata";
            try
            {
                ILiteCollection<DomainObjectMetadata<TIdentifier>> collection = _database.GetCollection<DomainObjectMetadata<TIdentifier>>(collectionName);
                collection.EnsureIndex(o => o.Id);
                DomainObjectMetadata<TIdentifier> domainObject = collection.Query()
                                                                           .Where(o => o.Id == identifier)
                                                                           .FirstOrDefault();

                // metadata should always be available, if not always complete
                return Task.FromResult(
                    domainObject is { }
                        ? Result.Success(domainObject.Metadata as IDictionary<string, string>)
                        : Result.Success(new Dictionary<string, string>() as IDictionary<string, string>));
            }
            catch (Exception e)
            {
                _logger.LogWarning(
                    e,
                    "unable to load metadata for domainObject from collection '{CollectionName}'",
                    collectionName);
                return Task.FromResult(
                    Result.Error<IDictionary<string, string>>(
                        $"unable to load metadata for domainObject from collection '{collectionName}'",
                        ErrorCode.DbUpdateError));
            }
        }

        /// <inheritdoc />
        public async Task<IResult> Remove<TObject, TIdentifier>(TIdentifier identifier)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
        {
            try
            {
                _memoryCache.Remove(identifier.ToString());
                ObjectLookup<TIdentifier> @object = await GetObjectInfo<TObject, TIdentifier>(identifier);
                long maxExistingVersion = @object.Versions.Any()
                                              ? @object.Versions
                                                       .Where(kvp => !kvp.Value.IsMarkedDeleted)
                                                       .Select(kvp => kvp.Key)
                                                       .Max()
                                              : -1;
                await RemoveObjectVersion<TObject, TIdentifier>(identifier, maxExistingVersion);

                return Result.Success();
            }
            catch (Exception e)
            {
                _logger.LogWarning(
                    e,
                    "unable to mark domainObject with id '{Identifier}' as deleted",
                    identifier);
                return Result.Error(
                    $"unable to mark domainObject with id '{identifier}' as deleted",
                    ErrorCode.DbUpdateError);
            }
        }

        /// <inheritdoc />
        public Task<IResult> SetProjectedVersion(string eventId, long eventVersion, string eventType)
        {
            try
            {
                ILiteCollection<StorageMetadata> collection = _database.GetCollection<StorageMetadata>();
                collection.EnsureIndex(o => o.Id);
                var entry = new StorageMetadata
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow,
                    LastWrittenEvent = eventVersion,
                    LastWrittenEventId = eventId,
                    LastWrittenEventType = eventType
                };

                collection.Insert(entry);
                return Task.FromResult(Result.Success());
            }
            catch (Exception e)
            {
                _logger.LogWarning(
                    e,
                    "unable to update projection metadata; eventId={EventId}; eventVersion={EventVersion}; eventType={EventType}",
                    eventId,
                    eventVersion,
                    eventType);
                return Task.FromResult(
                    Result.Error(
                        $"unable to update projection metadata; eventId={eventId}; eventVersion={eventVersion}; eventType={eventType}",
                        ErrorCode.DbUpdateError));
            }
        }

        /// <inheritdoc />
        public async Task<IResult> Store<TObject, TIdentifier>(TObject domainObject)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
        {
            string collectionName = typeof(TObject).Name;
            try
            {
                await RecordObjectVersion<TObject, TIdentifier>(domainObject);
                await _fileStore.StoreObject<TObject, TIdentifier>(domainObject);
                _memoryCache.Set(domainObject.Id.ToString(), domainObject, TimeSpan.FromSeconds(30));
            }
            catch (Exception e)
            {
                _logger.LogWarning(
                    e,
                    "unable to update/insert domainObject into collection '{CollectionName}'",
                    collectionName);
                return Result.Error(
                    $"unable to update/insert domainObject into collection '{collectionName}'",
                    ErrorCode.DbUpdateError);
            }

            if (_historyConfiguration.Value.RemoveOldVersions)
            {
                try
                {
                    ObjectLookup<TIdentifier> info = await GetObjectInfo<TObject, TIdentifier>(domainObject.Id);
                    List<long> versionsToRemove = info.Versions
                                                      .OrderByDescending(kvp => kvp.Key)
                                                      // skip the items that we want to remain
                                                      .Skip(_historyConfiguration.Value.RetainVersions)
                                                      // select all items that are not already deleted
                                                      .Where(kvp => kvp.Value.IsDataAvailable)
                                                      .Select(kvp => kvp.Key)
                                                      .ToList();

                    foreach (long version in versionsToRemove)
                    {
                        await _fileStore.DeleteObject<TObject, TIdentifier>(domainObject.Id, version);
                        await RemoveObjectVersionData<TObject, TIdentifier>(domainObject.Id, version);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "unable to trim versions of '{Identifier}'", domainObject.Id);
                }
            }

            return Result.Success();
        }

        /// <inheritdoc />
        public async Task<IResult> StoreMetadata<TObject, TIdentifier>(TObject domainObject, IDictionary<string, string> metadata)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
        {
            string collectionName = typeof(TObject).Name;
            string metadataCollectionName = typeof(TObject).Name + "_Metadata";
            try
            {
                // prevent metadata-entries for objects that are not stored yet
                IResult<Page<TIdentifier>> versionResult = await ListAll<TObject, TIdentifier>(QueryRange.All);

                if (versionResult.IsError)
                    return versionResult;

                Page<TIdentifier> page = versionResult.Data;
                IList<TIdentifier> versions = page.Items;
                bool domainObjectAvailable = versions.Any();

                if (!domainObjectAvailable)
                {
                    _logger.LogWarning(
                        "attempted to update/insert metadata for domainObject for which no version is stored",
                        collectionName);
                    return Result.Error(
                        "attempted to update/insert metadata for domainObject for which no version is stored",
                        ErrorCode.NotFound);
                }

                ILiteCollection<DomainObjectMetadata<TIdentifier>> collection =
                    _database.GetCollection<DomainObjectMetadata<TIdentifier>>(metadataCollectionName);
                collection.EnsureIndex(o => o.Id);

                collection.Upsert(
                    new DomainObjectMetadata<TIdentifier>
                    {
                        Id = domainObject.Id,
                        Metadata = metadata.ToDictionary(_ => _.Key, _ => _.Value)
                    });
            }
            catch (Exception e)
            {
                _logger.LogWarning(
                    e,
                    "unable to update/insert metadata for domainObject into collection '{CollectionName}'",
                    metadataCollectionName);
                return Result.Error(
                    $"unable to update/insert metadata for domainObject into collection '{metadataCollectionName}'",
                    ErrorCode.DbUpdateError);
            }

            return Result.Success();
        }

        private Task<ObjectLookup<TIdentifier>> GetObjectInfo<TObject, TIdentifier>(TIdentifier identifier)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
        {
            ILiteCollection<ObjectLookup<TIdentifier>> collection = GetLookupInfo<TIdentifier>();

            ObjectLookup<TIdentifier> @object =
                collection.Query()
                          .Where(x => x.Id == identifier)
                          .FirstOrDefault()
                ?? new ObjectLookup<TIdentifier>
                {
                    Id = identifier,
                    Versions = new Dictionary<long, ObjectLookupInfo>()
                };

            return Task.FromResult(@object);
        }

        private Task<List<ObjectLookup<TIdentifier>>> GetObjectInfo<TObject, TIdentifier>(
            Expression<Func<ObjectLookup<TIdentifier>, bool>> filter = null)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
        {
            ILiteCollection<ObjectLookup<TIdentifier>> collection = GetLookupInfo<TIdentifier>();

            List<ObjectLookup<TIdentifier>> objectInfo = filter is null
                                                             ? collection.Query().ToList()
                                                             : collection.Query().Where(filter).ToList();

            return Task.FromResult(objectInfo);
        }

        private async Task RecordObjectVersion<TObject, TIdentifier>(TObject domainObject)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
        {
            ILiteCollection<ObjectLookup<TIdentifier>> collection = GetLookupInfo<TIdentifier>();

            ObjectLookup<TIdentifier> @object = await GetObjectInfo<TObject, TIdentifier>(domainObject.Id);

            @object.Versions[domainObject.CurrentVersion] = new ObjectLookupInfo
            {
                IsDataAvailable = true,
                IsMarkedDeleted = false
            };

            collection.Upsert(@object);
        }

        private async Task RemoveObjectVersion<TObject, TIdentifier>(TIdentifier identifier, long version)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
        {
            ILiteCollection<ObjectLookup<TIdentifier>> collection = GetLookupInfo<TIdentifier>();

            ObjectLookup<TIdentifier> @object = await GetObjectInfo<TObject, TIdentifier>(identifier);

            if (!@object.Versions.Any())
            {
                return;
            }

            ObjectLookupInfo info = @object.Versions[version];
            info.IsMarkedDeleted = true;

            collection.Upsert(@object);
        }

        private async Task RemoveObjectVersionData<TObject, TIdentifier>(TIdentifier identifier, long version)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
        {
            ILiteCollection<ObjectLookup<TIdentifier>> collection = GetLookupInfo<TIdentifier>();

            ObjectLookup<TIdentifier> @object = await GetObjectInfo<TObject, TIdentifier>(identifier);

            if (!@object.Versions.Any())
            {
                return;
            }

            ObjectLookupInfo info = @object.Versions[version];
            info.IsDataAvailable = false;

            collection.Upsert(@object);
        }

        private ILiteCollection<ObjectLookup<TIdentifier>> GetLookupInfo<TIdentifier>()
            where TIdentifier : Identifier
            => _database.GetCollection<ObjectLookup<TIdentifier>>(
                typeof(ObjectLookup<TIdentifier>).GetFriendlyName()
                                                 .Replace('<', '_')
                                                 .Replace(">", string.Empty));

        /// <summary>
        ///     Additional metadata that is stored in a separate collection along the projected DomainObjects.
        ///     These Records aren't meant to be updated, only written.
        /// </summary>
        private class StorageMetadata
        {
            /// <summary>
            ///     Timestamp when this entry was written
            /// </summary>
            public DateTime CreatedAt { get; set; }

            /// <summary>
            ///     Unique Id for this entry
            /// </summary>
            public Guid Id { get; set; }

            /// <summary>
            ///     last DomainEvent that was projected and written to this Store
            /// </summary>
            public long LastWrittenEvent { get; set; }

            /// <summary>
            ///     generic Id of the last event that was written
            /// </summary>
            public string LastWrittenEventId { get; set; }

            /// <summary>
            ///     Type of the last event that was written
            /// </summary>
            public string LastWrittenEventType { get; set; }
        }

        /// <summary>
        ///     Entry for a DomainObject that has been stored
        /// </summary>
        private class ObjectLookup<TIdentifier>
            where TIdentifier : Identifier

        {
            /// <summary>
            ///     Identifier of the stored DomainObject
            /// </summary>
            public TIdentifier Id { get; set; }

            /// <summary>
            ///     Map of Versions and their current Status. True = Object exists, False = Object was deleted
            /// </summary>
            public Dictionary<long, ObjectLookupInfo> Versions { get; set; }
        }

        /// <summary>
        ///     Information for a DomainObject
        /// </summary>
        private class ObjectLookupInfo
        {
            /// <summary>
            ///     Flag to show if DomainObject is currently marked as deleted
            /// </summary>
            public bool IsMarkedDeleted { get; set; } = false;

            /// <summary>
            ///     Flag to show if actual Data for this DomainObject is available 
            /// </summary>
            public bool IsDataAvailable { get; set; } = false;
        }

        /// <summary>
        ///     Additional metadata for a given DomainObject
        /// </summary>
        /// <typeparam name="TIdentifier">identifier of the associated DomainObject</typeparam>
        private class DomainObjectMetadata<TIdentifier>
            where TIdentifier : Identifier
        {
            /// <summary>
            ///     Identifier of the associated DomainObject
            /// </summary>
            public TIdentifier Id { get; set; }

            /// <summary>
            ///     Generic, untyped metadata for the associated DomainObject
            /// </summary>
            public Dictionary<string, string> Metadata { get; set; }
        }
    }
}
