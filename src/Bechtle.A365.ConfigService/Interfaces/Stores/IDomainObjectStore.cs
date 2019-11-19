using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.DomainObjects;

namespace Bechtle.A365.ConfigService.Interfaces.Stores
{
    /// <summary>
    ///     Store to retrieve the latest available Version of a <see cref="DomainObject" />
    /// </summary>
    public interface IDomainObjectStore
    {
        /// <summary>
        ///     get the latest version of a simple <see cref="DomainObject" />
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        Task<IResult<T>> ReplayObject<T>() where T : DomainObject, new();

        /// <summary>
        ///     get the latest version of a simple <see cref="DomainObject" />, up to <paramref name="maxVersion" />
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="maxVersion">inclusive upper Version-Limit</param>
        /// <returns></returns>
        Task<IResult<T>> ReplayObject<T>(long maxVersion) where T : DomainObject, new();

        /// <summary>
        ///     stream the given <see cref="DomainObject" /> to its latest version,
        ///     identified by <paramref name="identifier" />
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="streamedObject"></param>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<IResult<T>> ReplayObject<T>(T streamedObject, string identifier) where T : DomainObject;

        /// <summary>
        ///     stream the given <see cref="DomainObject" /> to its latest version,
        ///     identified by <paramref name="identifier" />, up to <paramref name="maxVersion" />
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="streamedObject"></param>
        /// <param name="identifier"></param>
        /// <param name="maxVersion"></param>
        /// <returns></returns>
        Task<IResult<T>> ReplayObject<T>(T streamedObject, string identifier, long maxVersion) where T : DomainObject;
    }
}