using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Bechtle.A365.ConfigService.Common.Converters
{
    public class DictToJsonConverter
    {
        public JToken ToJson(IDictionary<string, string> dict)
        {
            if (!dict.Any())
                return new JObject();

            var root = new Node();

            // generate tree of Nodes
            foreach (var entry in dict.OrderBy(k => k.Key))
            {
                var key = entry.Key;
                var value = entry.Value;

                var pathParts = new Queue<string>(key.Split('/'));
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

            var jToken = CreateToken(root);

            return jToken;
        }

        private JArray CreateArray(Node node) => new JArray(node.Children
                                                                .OrderBy(c => int.Parse(c.Name))
                                                                .Select(CreateToken));

        private JObject CreateObject(Node node) => new JObject(node.Children
                                                                   .Select(c => new JProperty(UnEscapePath(c.Name),
                                                                                              CreateToken(c))));

        private JToken CreateToken(Node node)
        {
            if (!node.Children.Any())
                return CreateValue(node);

            if (node.Children.All(c => int.TryParse(c.Name, out _)))
                return CreateArray(node);

            return CreateObject(node);
        }

        private JValue CreateValue(Node node) => new JValue(node.Value);

        private string UnEscapePath(string p) => Uri.UnescapeDataString(p);

        private class Node
        {
            public List<Node> Children { get; } = new List<Node>();

            public string Name { get; set; } = string.Empty;

            public string Value { get; set; } = string.Empty;
        }
    }
}