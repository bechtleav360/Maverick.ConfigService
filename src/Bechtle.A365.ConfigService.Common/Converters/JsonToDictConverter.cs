using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Bechtle.A365.ConfigService.Common.Converters
{
    public class JsonToDictConverter
    {
        public static IDictionary<string, string> ToDict(JsonElement json, string separator, bool encodePath)
        {
            var dict = new Dictionary<string, string>();

            Visit(json, string.Empty, dict, encodePath);

            if (separator is null || separator.Equals("/", StringComparison.OrdinalIgnoreCase))
                return dict;

            return dict.ToDictionary(kvp => kvp.Key.Replace("/", separator),
                                     kvp => kvp.Value);
        }

        /// <summary>
        ///     escape the given path according to <see cref="encodePath" />
        /// </summary>
        /// <param name="p"></param>
        /// <param name="encodePath">if true, the whole path will be fully encoded. if false, only "/" will be replaced with %2F</param>
        /// <returns></returns>
        private static string EscapePath(string p, bool encodePath)
            => encodePath
                   ? Uri.EscapeDataString(p)
                   : p.Replace("/", "%2F");

        private static string MakeNextPath(string currentPath, string nextPath, bool encodePath)
            => string.IsNullOrWhiteSpace(currentPath)
                   ? EscapePath(nextPath, encodePath)
                   : $"{currentPath}/{EscapePath(nextPath, encodePath)}";

        private static void Visit(JsonElement json, string currentPath, IDictionary<string, string> dict, bool encodePath)
        {
            switch (json.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in json.EnumerateObject()
                                                 .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                                                 .ThenBy(p => p.Value.ValueKind))
                        Visit(property, currentPath, dict, encodePath);

                    break;

                case JsonValueKind.Array:
                    var index = 0;
                    foreach (var item in json.EnumerateArray())
                    {
                        Visit(item, MakeNextPath(currentPath, index.ToString("D4"), encodePath), dict, encodePath);
                        ++index;
                    }

                    break;

                case JsonValueKind.String:
                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                case JsonValueKind.Null:
                    dict[currentPath] = json.ToString();
                    break;

                // ReSharper disable once RedundantCaseLabel
                // disable, because i want to be explicit about what happens when
                case JsonValueKind.Undefined:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void Visit(JsonProperty property, string currentPath, IDictionary<string, string> dict, bool encodePath)
            => Visit(property.Value, MakeNextPath(currentPath, property.Name, encodePath), dict, encodePath);
    }
}