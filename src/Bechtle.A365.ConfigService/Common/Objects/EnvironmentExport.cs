using System;
using System.Linq;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.Common.Objects
{
    /// <summary>
    ///     a single Environment, exported for later import
    /// </summary>
    public record EnvironmentExport
    {
        /// <summary>
        /// </summary>
        public string Category { get; init; } = string.Empty;

        /// <summary>
        /// </summary>
        public LayerIdentifier[] Layers { get; init; } = Array.Empty<LayerIdentifier>();

        /// <summary>
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <inheritdoc />
        public virtual bool Equals(EnvironmentExport? other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Category == other.Category
                   && Name == other.Name
                   && Layers.SequenceEqual(other.Layers);
        }

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Category, Name, Layers);
    }
}
