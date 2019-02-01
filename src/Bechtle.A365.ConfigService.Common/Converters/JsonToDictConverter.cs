using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Bechtle.A365.ConfigService.Common.Converters
{
    public class JsonToDictConverter
    {
        public IDictionary<string, string> ToDict(JToken json, string separator)
        {
            var dict = new Dictionary<string, string>();

            if (json == null)
                return dict;

            Visit(json, string.Empty, dict);

            if (separator is null || separator.Equals("/", StringComparison.OrdinalIgnoreCase))
                return dict;

            return dict.ToDictionary(kvp => kvp.Key.Replace("/", separator),
                                     kvp => kvp.Value);
        }

        private string EscapePath(string p) => Uri.EscapeDataString(p);

        private string MakeNextPath(string currentPath, string nextPath) => string.IsNullOrWhiteSpace(currentPath)
                                                                                ? EscapePath(nextPath)
                                                                                : $"{currentPath}/{EscapePath(nextPath)}";

        private void Visit(JToken token, string currentPath, IDictionary<string, string> dict)
        {
            switch (token)
            {
                case null:
                    throw new ArgumentNullException($"token is null, Current Path: {currentPath}");

                case JArray jArray:
                    Visit(jArray, currentPath, dict);
                    break;

                case JObject jObject:
                    Visit(jObject, currentPath, dict);
                    break;

                case JProperty jProperty:
                    Visit(jProperty, currentPath, dict);
                    break;

                // apparently this also handles JRaw
                case JValue jValue:
                    Visit(jValue, currentPath, dict);
                    break;

                default:
                    throw new NotImplementedException($"handling of '{token.Type}' is not implemented");
            }
        }

        private void Visit(JArray jArray, string currentPath, IDictionary<string, string> dict)
        {
            var index = 0;
            foreach (var item in jArray.Children())
            {
                Visit(item, MakeNextPath(currentPath, index.ToString("D4")), dict);

                ++index;
            }
        }

        private void Visit(JObject jObject, string currentPath, IDictionary<string, string> dict)
        {
            foreach (var property in jObject.Properties()
                                            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                                            .ThenBy(p => p.Type))
                Visit(property, currentPath, dict);
        }

        private void Visit(JProperty jProperty, string currentPath, IDictionary<string, string> dict)
            => Visit(jProperty.Value, MakeNextPath(currentPath, jProperty.Name), dict);

        private void Visit(JValue jValue, string currentPath, IDictionary<string, string> dict)
            => dict[currentPath] = jValue?.Value?.ToString();
    }
}