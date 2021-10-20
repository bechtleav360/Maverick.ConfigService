using System.Collections.Generic;
using System.Linq;

namespace Bechtle.A365.ConfigService.Common.Utilities
{
    /// <summary>
    ///     Extensions to make it easier to work with <see cref="IDictionary{TKey,TValue}" /> or <see cref="Dictionary{TKey,TValue}" />
    /// </summary>
    public static class DictionaryExtensions
    {
        /// <summary>
        ///     Shortcut to translate an <see cref="IDictionary{TKey,TValue}" /> to an equivalent <see cref="Dictionary{TKey,TValue}" />
        /// </summary>
        /// <param name="dictionary">dictionary known only through its interface</param>
        /// <typeparam name="TKey">any Key-type</typeparam>
        /// <typeparam name="TValue">any Value-Type</typeparam>
        /// <returns>equivalent object, but as <see cref="Dictionary{TKey,TValue}" /></returns>
        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
            where TKey : notnull
        {
            if (dictionary is Dictionary<TKey, TValue> castedInstance)
            {
                return castedInstance;
            }

            return dictionary.ToDictionary(_ => _.Key, _ => _.Value);
        }
    }
}
