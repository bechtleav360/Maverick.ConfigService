using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Bechtle.A365.ConfigService.Common.Converters
{
    /// <summary>
    ///     Component that converts a Map of Paths/Values to their equivalent Json-Representation
    /// </summary>
    public class DictToJsonConverter
    {
        /// <summary>
        ///     Converts a <see cref="Dictionary{String, String}"/> to its Json-Representation, using the Keys as Paths
        /// </summary>
        /// <param name="dict">Dictionary containing paths ("Foo/Bar/Baz") to values ("42")</param>
        /// <param name="separator">separator used to split parts in the Path</param>
        /// <returns>the equivalent Json-Representation of the data in <paramref name="dict"/></returns>
        public static JsonElement ToJson(IDictionary<string, string> dict, string separator)
        {
            if (!dict.Any())
                return JsonDocument.Parse("{}").RootElement;

            var root = ConvertToTree(dict, separator);

            using var memoryStream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(memoryStream, new JsonWriterOptions { Indented = true }))
            {
                CreateTokenNative(root, writer);
            }

            memoryStream.Position = 0;
            return JsonDocument.Parse(memoryStream).RootElement;
        }

        /// <summary>
        ///     Converts a <see cref="Dictionary{String, String}"/> to its Json-Representation, using the Keys as Paths
        /// </summary>
        /// <param name="dict">Dictionary containing paths ("Foo/Bar/Baz") to values ("42")</param>
        /// <param name="separator">separator used to split parts in the Path</param>
        /// <returns>the equivalent Json-Representation of the data in <paramref name="dict"/></returns>
        public static JsonElement ToJson(ICollection<KeyValuePair<string, string>> dict, string separator)
        {
            if (!dict.Any())
                return JsonDocument.Parse("{}").RootElement;

            var root = ConvertToTree(dict, separator);

            using var memoryStream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(memoryStream, new JsonWriterOptions { Indented = true }))
            {
                CreateTokenNative(root, writer);
            }

            memoryStream.Position = 0;
            return JsonDocument.Parse(memoryStream).RootElement;
        }

        private static Node ConvertToTree(IEnumerable<KeyValuePair<string, string>> dict, string separator)
        {
            var root = new Node();

            // generate tree of Nodes
            foreach (var (key, value) in dict.OrderBy(k => k.Key))
            {
                var pathParts = new Queue<string>(key.Split(separator));
                var currentNode = root;

                // walk the path and create new nodes as necessary
                while (pathParts.TryDequeue(out var pathPart))
                {
                    var match = currentNode.Children.FirstOrDefault(c => c.Name.Equals(pathPart, StringComparison.OrdinalIgnoreCase));

                    // no node found, create new child with next path
                    // and continue from there
                    if (match is null)
                    {
                        var nextNode = new Node
                        {
                            Name = pathPart
                        };
                        currentNode.Children.Add(nextNode);
                        currentNode = nextNode;
                        continue;
                    }

                    // node found, continue from there
                    currentNode = match;
                }

                // walked the path to the deepest node, insert actual value
                currentNode.Value = value;
            }

            return root;
        }

        private static void CreateTokenNative(Node node, Utf8JsonWriter jsonStream)
        {
            try
            {
                if (!node.Children.Any())
                {
                    if (long.TryParse(node.Value, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out var l))
                        jsonStream.WriteNumberValue(l);
                    else if (double.TryParse(node.Value, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out var d))
                        jsonStream.WriteNumberValue(d);
                    else
                        jsonStream.WriteStringValue(node.Value);
                }
                else if (node.Children.All(c => int.TryParse(c.Name, out _)))
                {
                    jsonStream.WriteStartArray();

                    foreach (var child in node.Children.OrderBy(c => c.Name))
                    {
                        if (child.Children.Count == 0)
                        {
                            if (long.TryParse(node.Value, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out var l))
                                jsonStream.WriteNumberValue(l);
                            else if (double.TryParse(node.Value, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out var d))
                                jsonStream.WriteNumberValue(d);
                            else
                                jsonStream.WriteStringValue(child.Value);
                        }
                        else
                            CreateTokenNative(child, jsonStream);
                    }

                    jsonStream.WriteEndArray();
                }
                else
                {
                    jsonStream.WriteStartObject();

                    foreach (var child in node.Children)
                    {
                        jsonStream.WritePropertyName(UnEscapePath(child.Name));
                        CreateTokenNative(child, jsonStream);
                    }

                    jsonStream.WriteEndObject();
                }
            }
            catch (Exception e)
            {
                throw new JsonException($"error occured during json-translation at level: '{node.Name}'. see InnerException for more information", e);
            }
        }

        private static string UnEscapePath(string p) => Uri.UnescapeDataString(p);

        private class Node
        {
            public List<Node> Children { get; } = new List<Node>();

            public string Name { get; set; } = string.Empty;

            public string Value { get; set; } = string.Empty;
        }
    }
}
