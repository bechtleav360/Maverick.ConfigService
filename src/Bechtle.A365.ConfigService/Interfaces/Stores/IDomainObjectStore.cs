using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Models.V1;

namespace Bechtle.A365.ConfigService.Interfaces.Stores
{
    /// <summary>
    ///     Store to retrieve the latest available Version of a <see cref="DomainObject{TIdentifier}" />
    /// </summary>
    public interface IDomainObjectStore : IDisposable, IAsyncDisposable
    {
        /// <summary>
        ///     Check the store for the highest projected version (using <see cref="DomainObject{T}.CurrentVersion" />)
        /// </summary>
        /// <returns>the last event-number that was projected, or a failed result</returns>
        Task<IResult<long>> GetProjectedVersion();

        /// <summary>
        ///     List all objects of type <typeparamref name="TObject" />
        /// </summary>
        /// <param name="range">range of items to retrieve</param>
        /// <typeparam name="TObject">type of object to list</typeparam>
        /// <typeparam name="TIdentifier">identifier used by <typeparamref name="TObject" /></typeparam>
        /// <returns>result of the operation</returns>
        Task<IResult<Page<TIdentifier>>> ListAll<TObject, TIdentifier>(QueryRange range)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier;

        /// <summary>
        ///     List all objects of type <typeparamref name="TObject" />
        /// </summary>
        /// <param name="filter">expression used to filter the result-set</param>
        /// <param name="range">range of items to retrieve</param>
        /// <typeparam name="TObject">type of object to list</typeparam>
        /// <typeparam name="TIdentifier">identifier used by <typeparamref name="TObject" /></typeparam>
        /// <returns>result of the operation</returns>
        Task<IResult<Page<TIdentifier>>> ListAll<TObject, TIdentifier>(Func<TIdentifier, bool> filter, QueryRange range)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier;

        /// <summary>
        ///     List all objects of type <typeparamref name="TObject" />
        /// </summary>
        /// <param name="version">max version to search through</param>
        /// <param name="range">range of items to retrieve</param>
        /// <typeparam name="TObject">type of object to list</typeparam>
        /// <typeparam name="TIdentifier">identifier used by <typeparamref name="TObject" /></typeparam>
        /// <returns>result of the operation</returns>
        Task<IResult<Page<TIdentifier>>> ListAll<TObject, TIdentifier>(long version, QueryRange range)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier;

        /// <summary>
        ///     List all objects of type <typeparamref name="TObject" />
        /// </summary>
        /// <param name="version">max version to search through</param>
        /// <param name="filter">expression used to filter the result-set</param>
        /// <param name="range">range of items to retrieve</param>
        /// <typeparam name="TObject">type of object to list</typeparam>
        /// <typeparam name="TIdentifier">identifier used by <typeparamref name="TObject" /></typeparam>
        /// <returns>result of the operation</returns>
        Task<IResult<Page<TIdentifier>>> ListAll<TObject, TIdentifier>(long version, Func<TIdentifier, bool> filter, QueryRange range)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier;

        /// <summary>
        ///     Load a previously stored instance of a DomainObject from the underlying store
        /// </summary>
        /// <param name="identifier">public identifier for the desired DomainObject</param>
        /// <typeparam name="TObject">Subtype of <see cref="DomainObject{T}" /></typeparam>
        /// <typeparam name="TIdentifier">Type of Id the DomainObject uses</typeparam>
        /// <returns>result of the load-operation</returns>
        Task<IResult<TObject>> Load<TObject, TIdentifier>(TIdentifier identifier)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier;

        /// <summary>
        ///     Load a previously stored instance of a DomainObject from the underlying store, or the nearest one below the given version
        /// </summary>
        /// <param name="identifier">public identifier for the desired DomainObject</param>
        /// <param name="maxVersion">highest version that can be retrieved from the store</param>
        /// <typeparam name="TObject">Subtype of <see cref="DomainObject{T}" /></typeparam>
        /// <typeparam name="TIdentifier">Type of Id the DomainObject uses</typeparam>
        /// <returns>result of the load-operation</returns>
        Task<IResult<TObject>> Load<TObject, TIdentifier>(TIdentifier identifier, long maxVersion)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier;

        /// <summary>
        ///     Load the metadata for a previously stored instance of a DomainObject from the underlying store
        /// </summary>
        /// <param name="identifier">public identifier for the desired DomainObject</param>
        /// <typeparam name="TObject">Subtype of <see cref="DomainObject{T}" /></typeparam>
        /// <typeparam name="TIdentifier">Type of Id the DomainObject uses</typeparam>
        /// <returns>result of the load-operation</returns>
        Task<IResult<IDictionary<string, string>>> LoadMetadata<TObject, TIdentifier>(TIdentifier identifier)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier;

        /// <summary>
        ///     Remove a DomainObject from the store
        /// </summary>
        /// <param name="identifier">public identifier for the desired DomainObject</param>
        /// <typeparam name="TObject">Subtype of <see cref="DomainObject{T}" /></typeparam>
        /// <typeparam name="TIdentifier">Type of Id the DomainObject uses</typeparam>
        /// <returns>result of the store-operation</returns>
        Task<IResult> Remove<TObject, TIdentifier>(TIdentifier identifier)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier;

        /// <summary>
        ///     Set the last event# that was projected, and from which a new Projection should continue
        /// </summary>
        /// <param name="eventId">generic event-id</param>
        /// <param name="eventVersion">event-number within the stream</param>
        /// <param name="eventType">type of event that was projected</param>
        /// <returns>result of the store-operation</returns>
        Task<IResult> SetProjectedVersion(string eventId, long eventVersion, string eventType);

        /// <summary>
        ///     store / update the given DomainObject in the underlying store
        /// </summary>
        /// <param name="domainObject">valid instance of any DomainObject</param>
        /// <typeparam name="TObject">Subtype of <see cref="DomainObject{T}" /></typeparam>
        /// <typeparam name="TIdentifier">Type of Id the DomainObject uses</typeparam>
        /// <returns>result of the store-operation</returns>
        Task<IResult> Store<TObject, TIdentifier>(TObject domainObject)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier;

        /// <summary>
        ///     store / update the metadata of a given DomainObject in the underlying store
        /// </summary>
        /// <param name="domainObject">valid instance of any DomainObject</param>
        /// <param name="metadata">metadata to store for the given DomainObject</param>
        /// <typeparam name="TObject">Subtype of <see cref="DomainObject{T}" /></typeparam>
        /// <typeparam name="TIdentifier">Type of Id the DomainObject uses</typeparam>
        /// <returns>result of the store-operation</returns>
        Task<IResult> StoreMetadata<TObject, TIdentifier>(TObject domainObject, IDictionary<string, string> metadata)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier;
    }
}
