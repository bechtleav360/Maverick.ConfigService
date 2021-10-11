using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bechtle.A365.ConfigService.Common.Compilation
{
    /// <summary>
    ///     component that can query a configured store for a single or a range of keys
    /// </summary>
    public interface IConfigValueProvider
    {
        /// <summary>
        ///     get the value of a single key in the associated store
        /// </summary>
        /// <param name="path">full path to a single value</param>
        /// <returns></returns>
        Task<IResult<string?>> TryGetValue(string path);

        /// <summary>
        ///     get the values of all queried keys in the associated store
        /// </summary>
        /// <param name="query">query for a range of keys</param>
        /// <returns>dictionary of all matched keys relative to the query</returns>
        Task<IResult<Dictionary<string, string?>>> TryGetRange(string query);
    }
}
