using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Bechtle.A365.ConfigService.Common.Converters
{
    public class DictToJsonConverter
    {
        private static Node ConvertToTree(IDictionary<string, string> dict, string separator)
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
            if (!node.Children.Any())
            {
                jsonStream.WriteStringValue(node.Value);
            }
            else if (node.Children.All(c => int.TryParse(c.Name, out _)))
            {
                jsonStream.WriteStartArray();
                foreach (var child in node.Children.OrderBy(c => c.Name))
                {
                    jsonStream.WritePropertyName(child.Name);
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

                jsonStream.WriteEndArray();
            }
        }

        public string ToJsonNative(IDictionary<string, string> dict, string separator)
        {
            if (!dict.Any())
                return string.Empty;

            var root = ConvertToTree(dict, separator);

            using var memoryStream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(memoryStream, new JsonWriterOptions {Indented = true}))
            {
                CreateTokenNative(root, writer);
            }

            return Encoding.UTF8.GetString(memoryStream.ToArray());
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