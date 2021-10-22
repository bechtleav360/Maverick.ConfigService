using System;
using System.Linq;

namespace Bechtle.A365.ConfigService.Common.Objects
{
    /// <summary>
    ///     a single Layer, exported for later import
    /// </summary>
    public record LayerExport
    {
        /// <summary>
        ///     List of Keys currently contained in this Layer
        /// </summary>
        public EnvironmentKeyExport[] Keys { get; init; } = Array.Empty<EnvironmentKeyExport>();

        /// <summary>
        ///     Name of the exported Layer
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <inheritdoc />
        public virtual bool Equals(LayerExport? other)
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
                   && Keys.SequenceEqual(other.Keys);
        }

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Name, Keys);
    }
}
