using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using LiteDB;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Implementations.Stores
{
    /// <summary>
    ///     default ObjectStore using <see cref="ILiteDatabase" /> for storing objects
    /// </summary>
    public sealed class DomainObjectStore : IDomainObjectStore
    {
        private readonly ILiteDatabase _database;
        private readonly ILogger<DomainObjectStore> _logger;

        /// <inheritdoc cref="DomainObjectStore" />
        public DomainObjectStore(
            ILogger<DomainObjectStore> logger,
            IDomainObjectStoreLocationProvider locationProvider)
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

            _logger = logger;
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
        public Task<IResult<IList<TIdentifier>>> ListAll<TObject, TIdentifier>(QueryRange range)
            where TObject : DomainObject<TIdentifier> where TIdentifier : Identifier
        {
            string collectionName = typeof(TObject).Name;
            try
            {
                ILiteCollection<TObject> collection = _database.GetCollection<TObject>(collectionName);
                IList<TIdentifier> ids = collection.Query()
                                                   .OrderBy(o => o.Id)
                                                   .Select(o => o.Id)
                                                   .Skip(range.Offset)
                                                   .Limit(range.Length)
                                                   .ToList();

                return Task.FromResult(Result.Success(ids));
            }
            catch (Exception e)
            {
                _logger.LogWarning(
                    e,
                    "unable to list objects in collection '{CollectionName}'",
                    collectionName);
                return Task.FromResult(
                    Result.Error<IList<TIdentifier>>(
                        $"unable to list objects in collection '{collectionName}'",
                        ErrorCode.DbUpdateError));
            }
        }

        /// <inheritdoc />
        public Task<IResult<IList<TIdentifier>>> ListAll<TObject, TIdentifier>(Expression<Func<TObject, bool>> filter, QueryRange range)
            where TObject : DomainObject<TIdentifier> where TIdentifier : Identifier
        {
            string collectionName = typeof(TObject).Name;
            try
            {
                ILiteCollection<TObject> collection = _database.GetCollection<TObject>(collectionName);
                IList<TIdentifier> ids = collection.Query()
                                                   .OrderBy(o => o.Id)
                                                   .Where(filter)
                                                   .Select(o => o.Id)
                                                   .Skip(range.Offset)
                                                   .Limit(range.Length)
                                                   .ToList();

                return Task.FromResult(Result.Success(ids));
            }
            catch (Exception e)
            {
                _logger.LogWarning(
                    e,
                    "unable to list objects in collection '{CollectionName}'",
                    collectionName);
                return Task.FromResult(
                    Result.Error<IList<TIdentifier>>(
                        $"unable to list objects in collection '{collectionName}'",
                        ErrorCode.DbUpdateError));
            }
        }

        /// <inheritdoc />
        public Task<IResult<TObject>> Load<TObject, TIdentifier>(TIdentifier identifier)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
        {
            string collectionName = typeof(TObject).Name;
            try
            {
                ILiteCollection<TObject> collection = _database.GetCollection<TObject>(collectionName);
                TObject domainObject = collection.Query()
                                                 .Where(o => o.Id == identifier)
                                                 .OrderByDescending(o => o.MetaVersion)
                                                 .FirstOrDefault();

                if (domainObject is { })
                {
                    return Task.FromResult(Result.Success(domainObject));
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(
                    e,
                    "unable to load domainObject from collection '{CollectionName}'",
                    collectionName);
                return Task.FromResult(
                    Result.Error<TObject>(
                        $"unable to load domainObject from collection '{collectionName}'",
                        ErrorCode.DbUpdateError));
            }

            _logger.LogWarning(
                "no domainObject with id '{Identifier}' found in collection '{CollectionName}'",
                identifier.ToString(),
                collectionName);
            return Task.FromResult(
                Result.Error<TObject>(
                    $"no domainObject with id '{identifier}' found in collection '{collectionName}'",
                    ErrorCode.NotFound));
        }

        /// <inheritdoc />
        public Task<IResult<TObject>> Load<TObject, TIdentifier>(TIdentifier identifier, long maxVersion)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
        {
            string collectionName = typeof(TObject).Name;
            try
            {
                ILiteCollection<TObject> collection = _database.GetCollection<TObject>(collectionName);
                TObject domainObject = collection.Query()
                                                 .Where(o => o.Id == identifier)
                                                 .Where(o => o.CurrentVersion <= maxVersion)
                                                 .OrderByDescending(o => o.MetaVersion)
                                                 .FirstOrDefault();

                if (domainObject is { })
                {
                    return Task.FromResult(Result.Success(domainObject));
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(
                    e,
                    "unable to load domainObject from collection '{CollectionName}' with id '{Identifier}'",
                    collectionName,
                    identifier);
                return Task.FromResult(
                    Result.Error<TObject>(
                        $"unable to load domainObject from collection '{collectionName}' with id '{identifier}'",
                        ErrorCode.DbUpdateError));
            }

            _logger.LogWarning(
                "no domainObject with id '{Identifier}' found in collection '{CollectionName}'",
                identifier.ToString(),
                collectionName);
            return Task.FromResult(
                Result.Error<TObject>(
                    $"no domainObject with id '{identifier}' found in collection '{collectionName}'",
                    ErrorCode.NotFound));
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
        public Task<IResult> Remove<TObject, TIdentifier>(TIdentifier identifier)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
        {
            string collectionName = typeof(TObject).Name;
            string metadataCollectionName = typeof(TObject).Name + "_Metadata";
            try
            {
                ILiteCollection<TObject> collection = _database.GetCollection<TObject>(collectionName);
                collection.DeleteMany(o => o.Id == identifier);

                ILiteCollection<DomainObjectMetadata<TIdentifier>> metadataCollection =
                    _database.GetCollection<DomainObjectMetadata<TIdentifier>>(metadataCollectionName);
                metadataCollection.DeleteMany(e => e.Id == identifier);

                return Task.FromResult(Result.Success());
            }
            catch (Exception e)
            {
                _logger.LogWarning(
                    e,
                    "unable to remove domainObject from collection '{CollectionName}' with id '{Identifier}'",
                    collectionName,
                    identifier);
                return Task.FromResult(
                    Result.Error(
                        $"unable to remove domainObject from collection '{collectionName}' with id '{identifier}'",
                        ErrorCode.DbUpdateError));
            }
        }

        /// <inheritdoc />
        public Task<IResult> SetProjectedVersion(string eventId, long eventVersion, string eventType)
        {
            try
            {
                ILiteCollection<StorageMetadata> collection = _database.GetCollection<StorageMetadata>();
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
        public Task<IResult> Store<TObject, TIdentifier>(TObject domainObject)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
        {
            string collectionName = typeof(TObject).Name;
            try
            {
                ILiteCollection<TObject> collection = _database.GetCollection<TObject>(collectionName);
                collection.Upsert(domainObject);
            }
            catch (Exception e)
            {
                _logger.LogWarning(
                    e,
                    "unable to update/insert domainObject into collection '{CollectionName}'",
                    collectionName);
                return Task.FromResult(
                    Result.Error(
                        $"unable to update/insert domainObject into collection '{collectionName}'",
                        ErrorCode.DbUpdateError));
            }

            return Task.FromResult(Result.Success());
        }

        /// <inheritdoc />
        public Task<IResult> StoreMetadata<TObject, TIdentifier>(TObject domainObject, IDictionary<string, string> metadata)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
        {
            string collectionName = typeof(TObject).Name;
            string metadataCollectionName = typeof(TObject).Name + "_Metadata";
            try
            {
                // prevent metadata-entries for objects that are not stored yet
                ILiteCollection<TObject> objectCollection = _database.GetCollection<TObject>(collectionName);
                bool domainObjectAvailable = objectCollection.Query()
                                                             .Where(o => o.Id == domainObject.Id)
                                                             .OrderByDescending(o => o.MetaVersion)
                                                             .FirstOrDefault() is { };

                if (!domainObjectAvailable)
                {
                    _logger.LogWarning(
                        "attempted to update/insert metadata for domainObject that was not stored in collection '{CollectionName}'",
                        collectionName);
                    return Task.FromResult(
                        Result.Error(
                            $"attempted to update/insert metadata for domainObject that was not stored in collection '{collectionName}'",
                            ErrorCode.NotFound));
                }

                ILiteCollection<DomainObjectMetadata<TIdentifier>> collection =
                    _database.GetCollection<DomainObjectMetadata<TIdentifier>>(metadataCollectionName);

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
                return Task.FromResult(
                    Result.Error(
                        $"unable to update/insert metadata for domainObject into collection '{metadataCollectionName}'",
                        ErrorCode.DbUpdateError));
            }

            return Task.FromResult(Result.Success());
        }

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
