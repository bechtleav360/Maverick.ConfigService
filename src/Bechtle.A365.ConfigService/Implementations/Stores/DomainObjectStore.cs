using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
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
        private readonly ILogger<DomainObjectStore> _logger;
        private readonly ILiteDatabase _database;

        /// <inheritdoc cref="DomainObjectStore" />
        public DomainObjectStore(ILogger<DomainObjectStore> logger)
        {
            _logger = logger;
            _database = new LiteDatabase(
                new ConnectionString
                {
                    Connection = ConnectionType.Shared,
                    Filename = "data/projections.db"
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
                    "unable to update/insert domainObject into collection '{CollectionName}'",
                    collectionName);
                return Task.FromResult(
                    Result.Error<TObject>(
                        $"unable to update/insert domainObject into collection '{collectionName}'",
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
                    "unable to update/insert domainObject into collection '{CollectionName}' with id '{Identifier}'",
                    collectionName,
                    identifier);
                return Task.FromResult(
                    Result.Error<TObject>(
                        $"unable to update/insert domainObject into collection '{collectionName}' with id '{identifier}'",
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
        public Task<IResult> Remove<TObject, TIdentifier>(TIdentifier identifier)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
        {
            string collectionName = typeof(TObject).Name;
            try
            {
                ILiteCollection<TObject> collection = _database.GetCollection<TObject>(collectionName);
                collection.DeleteMany(o => o.Id == identifier);
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
        public Task<IResult<IList<TIdentifier>>> ListAll<TObject, TIdentifier>() where TObject : DomainObject<TIdentifier> where TIdentifier : Identifier
        {
            string collectionName = typeof(TObject).Name;
            try
            {
                ILiteCollection<TObject> collection = _database.GetCollection<TObject>(collectionName);
                IList<TIdentifier> ids = collection.Query()
                                                   .Select(o => o.Id)
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

        /// <summary>
        ///     Additional metadata that is stored in a separate collection along the projected DomainObjects.
        ///     These Records aren't meant to be updated, only written.
        /// </summary>
        private class StorageMetadata
        {
            /// <summary>
            ///     Unique Id for this entry
            /// </summary>
            public Guid Id { get; set; }

            /// <summary>
            ///     Timestamp when this entry was written
            /// </summary>
            public DateTime CreatedAt { get; set; }

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
    }
}
