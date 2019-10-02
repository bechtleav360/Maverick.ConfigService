using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Bechtle.A365.ConfigService.Common.Converters
{
    public interface IJsonTranslator
    {
        /// <summary>
        ///     counterpart to <see cref="ToJson(IDictionary{string,string})" />, converts json to a number of Key / Value pairs
        ///     <remarks>
        ///         {
        ///         "Some/Path/To/Somewhere" => "SomeValue",
        ///         "Endpoints/0000/Name" => "configuration"
        ///         }
        ///     </remarks>
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        IDictionary<string, string> ToDictionary(JToken json);

        /// <summary>
        ///     counterpart to <see cref="ToJson(IDictionary{string,string})" />, converts json to a number of Key / Value pairs
        ///     <remarks>
        ///         {
        ///         "Some/Path/To/Somewhere" => "SomeValue",
        ///         "Endpoints/0000/Name" => "configuration"
        ///         }
        ///     </remarks>
        /// </summary>
        /// <param name="json"></param>
        /// <param name="encodePath">true to fully encode the path</param>
        /// <returns></returns>
        IDictionary<string, string> ToDictionary(JToken json, bool encodePath);

        /// <summary>
        ///     counterpart to <see cref="ToJson(IDictionary{string,string}, string)" />, converts json to a number of Key / Value pairs
        ///     <remarks>
        ///         {
        ///         "Some/Path/To/Somewhere" => "SomeValue",
        ///         "Endpoints/0000/Name" => "configuration"
        ///         }
        ///     </remarks>
        /// </summary>
        /// <param name="json"></param>
        /// <param name="separator">separator to use in the keys</param>
        /// <returns></returns>
        IDictionary<string, string> ToDictionary(JToken json, string separator);

        /// <summary>
        ///     counterpart to <see cref="ToJson(IDictionary{string,string}, string)" />, converts json to a number of Key / Value pairs
        ///     <remarks>
        ///         {
        ///         "Some/Path/To/Somewhere" => "SomeValue",
        ///         "Endpoints/0000/Name" => "configuration"
        ///         }
        ///     </remarks>
        /// </summary>
        /// <param name="json"></param>
        /// <param name="separator">separator to use in the keys</param>
        /// <param name="encodePath">true to fully encode the path</param>
        /// <returns></returns>
        IDictionary<string, string> ToDictionary(JToken json, string separator, bool encodePath);

        /// <summary>
        ///     convert a dictionary of Paths=>Values to a JToken.
        ///     paths should look like this:
        ///     <remarks>
        ///         {
        ///         "Some/Path/To/Somewhere" => "SomeValue",
        ///         "Endpoints/0000/Name" => "configuration"
        ///         }
        ///     </remarks>
        /// </summary>
        /// <param name="dict"></param>
        /// <returns></returns>
        JToken ToJson(IDictionary<string, string> dict);

        /// <summary>
        ///     convert a dictionary of Paths=>Values to a JToken.
        ///     paths should look like this:
        ///     <remarks>
        ///         {
        ///         "Some/Path/To/Somewhere" => "SomeValue",
        ///         "Endpoints/0000/Name" => "configuration"
        ///         }
        ///     </remarks>
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        JToken ToJson(IDictionary<string, string> dict, string separator);
    }
}