using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.DomainObjects;

namespace Bechtle.A365.ConfigService.Services.Stores
{
    /// <summary>
    ///     Store to retrieve the latest available Version of a <see cref="StreamedObject"/>
    /// </summary>
    public interface IStreamedStore
    {
        /// <summary>
        ///     get the latest version of a simple <see cref="StreamedObject"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        Task<IResult<T>> GetStreamedObject<T>() where T : StreamedObject, new();

        /// <summary>
        ///     get the latest version of a simple <see cref="StreamedObject"/>, up to <paramref name="maxVersion"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="maxVersion">inclusive upper Version-Limit</param>
        /// <returns></returns>
        Task<IResult<T>> GetStreamedObject<T>(long maxVersion) where T : StreamedObject, new();

        /// <summary>
        ///     stream the given <see cref="StreamedObject"/> to its latest version,
        ///     identified by <paramref name="identifier"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="streamedObject"></param>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<IResult<T>> GetStreamedObject<T>(T streamedObject, string identifier) where T : StreamedObject;

        /// <summary>
        ///     stream the given <see cref="StreamedObject"/> to its latest version,
        ///     identified by <paramref name="identifier"/>, up to <paramref name="maxVersion"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="streamedObject"></param>
        /// <param name="identifier"></param>
        /// <param name="maxVersion"></param>
        /// <returns></returns>
        Task<IResult<T>> GetStreamedObject<T>(T streamedObject, string identifier, long maxVersion) where T : StreamedObject;
    }
}