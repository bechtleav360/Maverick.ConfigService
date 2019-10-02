﻿using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Bechtle.A365.ConfigService.Common.Converters
{
    public class JsonToDictConverter
    {
        public IDictionary<string, string> ToDict(JToken json, string separator, bool encodePath)
        {
            var dict = new Dictionary<string, string>();

            if (json == null)
                return dict;

            Visit(json, string.Empty, dict, encodePath);

            if (separator is null || separator.Equals("/", StringComparison.OrdinalIgnoreCase))
                return dict;

            return dict.ToDictionary(kvp => kvp.Key.Replace("/", separator),
                                     kvp => kvp.Value);
        }

        /// <summary>
        ///     escape the given path according to <see cref="encodePath"/>
        /// </summary>
        /// <param name="p"></param>
        /// <param name="encodePath">if true, the whole path will be fully encoded. if false, only "/" will be replaced with %2F</param>
        /// <returns></returns>
        private string EscapePath(string p, bool encodePath)
            => encodePath
                   ? Uri.EscapeDataString(p)
                   : p.Replace("/", "%2F");

        private string MakeNextPath(string currentPath, string nextPath, bool encodePath)
            => string.IsNullOrWhiteSpace(currentPath)
                   ? EscapePath(nextPath, encodePath)
                   : $"{currentPath}/{EscapePath(nextPath, encodePath)}";

        private void Visit(JToken token, string currentPath, IDictionary<string, string> dict, bool encodePath)
        {
            switch (token)
            {
                case null:
                    throw new ArgumentNullException($"token is null, Current Path: {currentPath}");

                case JArray jArray:
                    Visit(jArray, currentPath, dict, encodePath);
                    break;

                case JObject jObject:
                    Visit(jObject, currentPath, dict, encodePath);
                    break;

                case JProperty jProperty:
                    Visit(jProperty, currentPath, dict, encodePath);
                    break;

                // apparently this also handles JRaw
                case JValue jValue:
                    Visit(jValue, currentPath, dict);
                    break;

                default:
                    throw new NotImplementedException($"handling of '{token.Type}' is not implemented");
            }
        }

        private void Visit(JArray jArray, string currentPath, IDictionary<string, string> dict, bool encodePath)
        {
            var index = 0;
            foreach (var item in jArray.Children())
            {
                Visit(item, MakeNextPath(currentPath, index.ToString("D4"), encodePath), dict, encodePath);

                ++index;
            }
        }

        private void Visit(JObject jObject, string currentPath, IDictionary<string, string> dict, bool encodePath)
        {
            foreach (var property in jObject.Properties()
                                            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                                            .ThenBy(p => p.Type))
                Visit(property, currentPath, dict, encodePath);
        }

        private void Visit(JProperty jProperty, string currentPath, IDictionary<string, string> dict, bool encodePath)
            => Visit(jProperty.Value, MakeNextPath(currentPath, jProperty.Name, encodePath), dict, encodePath);

        private void Visit(JValue jValue, string currentPath, IDictionary<string, string> dict)
            => dict[currentPath] = jValue?.Value?.ToString();
    }
}