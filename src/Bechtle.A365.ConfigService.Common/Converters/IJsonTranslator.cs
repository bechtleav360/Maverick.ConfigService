using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Bechtle.A365.ConfigService.Common.Converters
{
    public interface IJsonTranslator
    {
        /// <summary>
        ///     counterpart to <see cref="ToJson" />, converts json to a number of Key / Value pairs
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
    }
}