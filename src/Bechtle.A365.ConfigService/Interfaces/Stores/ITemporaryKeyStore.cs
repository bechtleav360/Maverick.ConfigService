using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;

namespace Bechtle.A365.ConfigService.Interfaces.Stores
{
    /// <summary>
    ///     Store for temporary, high-priority Keys
    /// </summary>
    public interface ITemporaryKeyStore : IDisposable, IAsyncDisposable
    {
        /// <summary>
        ///     extend the lifespan of a temporary key in the store, and setting a new lifespan
        /// </summary>
        /// <param name="region"></param>
        /// <param name="key"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        Task<IResult> Extend(string region, string key, TimeSpan duration);

        /// <summary>
        ///     extend the lifespan of multiple keys in the store, and setting a new lifespan
        /// </summary>
        /// <param name="region"></param>
        /// <param name="keys"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        Task<IResult> Extend(string region, IEnumerable<string> keys, TimeSpan duration);

        /// <summary>
        ///     get the value of a temporary key from the store
        /// </summary>
        /// <param name="region"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        Task<IResult<string>> Get(string region, string key);

        /// <summary>
        ///     get values of multiple temporary keys from the store
        /// </summary>
        /// <param name="region"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        Task<IResult<IDictionary<string, string>>> Get(string region, IEnumerable<string> keys);

        /// <summary>
        ///     get all stored keys across all regions
        /// </summary>
        /// <returns></returns>
        Task<IResult<IDictionary<string, IDictionary<string, string>>>> GetAll();

        /// <summary>
        ///     get all stored keys for a specific region
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        Task<IResult<IDictionary<string, string>>> GetAll(string region);

        /// <summary>
        ///     remove a temporary key before its lifespan is over
        /// </summary>
        /// <param name="region"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        Task<IResult> Remove(string region, string key);

        /// <summary>
        ///     remove multiple keys from the store before their lifespan is over
        /// </summary>
        /// <param name="region"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        Task<IResult> Remove(string region, IEnumerable<string> keys);

        /// <summary>
        ///     add or update a temporary key in the store
        /// </summary>
        /// <param name="region"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="duration">sliding lifespan, can be extended through <see cref="Extend(string, string, TimeSpan)" /></param>
        /// <returns></returns>
        Task<IResult> Set(string region, string key, string value, TimeSpan duration);

        /// <summary>
        ///     add or update multiple keys with the same lifespan in the store
        /// </summary>
        /// <param name="region"></param>
        /// <param name="values"></param>
        /// <param name="duration">sliding lifespan, can be extended through <see cref="Extend(string, IEnumerable{string}, TimeSpan)" /></param>
        /// <returns></returns>
        Task<IResult> Set(string region, IDictionary<string, string> values, TimeSpan duration);
    }
}