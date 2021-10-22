using System;
using System.Linq;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.Common.Objects
{
    /// <summary>
    ///     definition of what should be exported
    /// </summary>
    public record ExportDefinition
    {
        /// <summary>
        ///     list of environments that should be exported
        /// </summary>
        public EnvironmentIdentifier[] Environments { get; init; } = Array.Empty<EnvironmentIdentifier>();

        /// <summary>
        ///     list of layers that should be exported
        /// </summary>
        public LayerIdentifier[] Layers { get; init; } = Array.Empty<LayerIdentifier>();

        /// <inheritdoc />
        public virtual bool Equals(ExportDefinition? other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Environments.SequenceEqual(other.Environments)
                   && Layers.SequenceEqual(other.Layers);
        }

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Environments, Layers);
    }
}
