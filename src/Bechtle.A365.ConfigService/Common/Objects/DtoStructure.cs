using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Bechtle.A365.ConfigService.Common.Objects
{
    /// <summary>
    ///     transfer-object containing information about a Configuration-Structure
    /// </summary>
    public record DtoStructure
    {
        /// <summary>
        ///     unique name of this Structure
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        ///     arbitrary json describing the desired configuration once it's built
        /// </summary>
        public JsonElement Structure { get; init; }

        /// <summary>
        ///     additional variables used while building the configuration
        /// </summary>
        public Dictionary<string, object> Variables { get; init; } = new();

        /// <summary>
        ///     unique version of this Structure
        /// </summary>
        public int Version { get; init; }

        /// <inheritdoc />
        public virtual bool Equals(DtoStructure? other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Name == other.Name
                   // as of .NetCore 3.1 JsonElement does not support deep-equality-comparison so we have to do it ourselves
                   // because i can't be bothered to do it the "Right" way, we do this:
                   && Structure.ToString().Equals(other.Structure.ToString(), StringComparison.OrdinalIgnoreCase)
                   && Variables.SequenceEqual(other.Variables)
                   && Version == other.Version;
        }

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Name, Structure, Variables, Version);
    }
}
