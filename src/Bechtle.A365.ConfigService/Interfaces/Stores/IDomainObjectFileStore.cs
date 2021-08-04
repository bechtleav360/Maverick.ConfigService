using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;

namespace Bechtle.A365.ConfigService.Interfaces.Stores
{
    /// <summary>
    ///     Component that stores DomainObjects in a local FileStructure
    /// </summary>
    public interface IDomainObjectFileStore
    {
        /// <summary>
        ///     Delete the file associated with a given DomainObject
        /// </summary>
        /// <param name="identifier">identifier with which the DomainObject was previously stored</param>
        /// <param name="version">Version of the DomainObject to delete</param>
        /// <typeparam name="TObject">Type of DomainObject to delete</typeparam>
        /// <typeparam name="TIdentifier">Type of Identifier used by <typeparamref name="TObject" /></typeparam>
        /// <returns>Result of the Operation</returns>
        public Task<IResult> DeleteObject<TObject, TIdentifier>(TIdentifier identifier, long version)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier;

        /// <summary>
        ///     Load a DomainObject from a local File
        /// </summary>
        /// <param name="identifier">identifier with which the DomainObject was previously stored</param>
        /// <param name="version">Version of the DomainObject to load</param>
        /// <typeparam name="TObject">Type of DomainObject to retrieve</typeparam>
        /// <typeparam name="TIdentifier">Type of Identifier used by <typeparamref name="TObject" /></typeparam>
        /// <returns>Result of the Operation with loaded DomainObject</returns>
        public Task<IResult<TObject>> LoadObject<TObject, TIdentifier>(TIdentifier identifier, long version)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier;

        /// <summary>
        ///     Store a DomainObject in a local File
        /// </summary>
        /// <param name="obj">DomainObject to store locally</param>
        /// <typeparam name="TObject">Type of DomainObject to retrieve</typeparam>
        /// <typeparam name="TIdentifier">Type of Identifier used by <typeparamref name="TObject" /></typeparam>
        /// <returns>Result of the Operation</returns>
        public Task<IResult> StoreObject<TObject, TIdentifier>(TObject obj)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier;
    }
}
