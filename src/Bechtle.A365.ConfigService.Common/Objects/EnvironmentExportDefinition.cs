using System.Collections.Generic;

namespace Bechtle.A365.ConfigService.Common.Objects
{
    public class EnvironmentExportDefinition
    {
        public EnvironmentExportDefinition() : this(string.Empty, string.Empty, new List<string>())
        {
        }

        public EnvironmentExportDefinition(string category, string name) : this(category, name, new List<string>())
        {
        }

        public EnvironmentExportDefinition(string category, string name, List<string> keys)
        {
            Category = category;
            Name = name;
            Keys = keys;
        }

        /// <summary>
        ///     Category for a group of Environments, think Folder / Tenant and the like
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        ///     list of keys that should be exported, if any.
        ///     use Empty list or null to export everything
        /// </summary>
        public List<string> Keys { get; set; }

        /// <summary>
        ///     Unique name for an Environment within a <see cref="Category" />
        /// </summary>
        public string Name { get; set; }
    }
}