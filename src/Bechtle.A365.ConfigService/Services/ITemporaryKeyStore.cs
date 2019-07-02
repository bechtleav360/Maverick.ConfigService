using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;

namespace Bechtle.A365.ConfigService.Services
{
    /// <summary>
    ///     Store for temporary, high-priority Keys
    /// </summary>
    public interface ITemporaryKeyStore
    {
        /// <summary>
        ///     extend the lifespan of a temporary key in the store
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        Task<IResult> Extend(string structure, string key);

        /// <summary>
        ///     extend the lifespan of multiple keys in the store
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        Task<IResult> Extend(string structure, IEnumerable<string> keys);

        /// <summary>
        ///     extend the lifespan of a temporary key in the store, and setting a new lifespan
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="key"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        Task<IResult> Extend(string structure, string key, TimeSpan duration);

        /// <summary>
        ///     extend the lifespan of multiple keys in the store, and setting a new lifespan
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="keys"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        Task<IResult> Extend(string structure, IEnumerable<string> keys, TimeSpan duration);

        /// <summary>
        ///     get the value of a temporary key from the store
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        Task<IResult<string>> Get(string structure, string key);

        /// <summary>
        ///     get values of multiple temporary keys from the store
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        Task<IResult<IDictionary<string, string>>> Get(string structure, IEnumerable<string> keys);

        /// <summary>
        ///     get all stored keys across all structures
        /// </summary>
        /// <returns></returns>
        Task<IResult<IDictionary<string, IDictionary<string, string>>>> GetAll();

        /// <summary>
        ///     get all stored keys for a specific structure
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        Task<IResult<IDictionary<string, string>>> GetAll(string structure);

        /// <summary>
        ///     remove a temporary key before its lifespan is over
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        Task<IResult> Remove(string structure, string key);

        /// <summary>
        ///     remove multiple keys from the store before their lifespan is over
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        Task<IResult> Remove(string structure, IEnumerable<string> keys);

        /// <summary>
        ///     add or update a temporary key in the store
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="duration">sliding lifespan, can be extended through <see cref="Extend(string)" /></param>
        /// <returns></returns>
        Task<IResult> Set(string structure, string key, string value, TimeSpan duration);

        /// <summary>
        ///     add or update multiple keys with the same lifespan in the store
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="values"></param>
        /// <param name="duration">sliding lifespan, can be extended through <see cref="Extend(string, string)" /></param>
        /// <returns></returns>
        Task<IResult> Set(string structure, IDictionary<string, string> values, TimeSpan duration);
    }
}