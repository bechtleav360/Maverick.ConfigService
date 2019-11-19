using System;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Bechtle.A365.ConfigService.Common.Utilities
{
    public static class DebugUtilities
    {
        public static string FormatConfiguration(IConfiguration config)
            => $"Raw Config-Keys:{Environment.NewLine}" +
               $"{FormatConfigurationRecursive(config)}";

        private static string FormatConfigurationRecursive(IConfiguration config, int indent = 0)
        {
            var builder = new StringBuilder();
            var indentBuilder = new StringBuilder();

            for (var i = 0; i < indent; ++i)
                indentBuilder.Append("| ");

            var indentString = indentBuilder.ToString();

            foreach (var child in config.GetChildren())
            {
                builder.Append($"{indentString}{child.Key}: {child.Value}{Environment.NewLine}");
                builder.Append(FormatConfigurationRecursive(child, indent + 2));
            }

            return builder.ToString();
        }
    }
}