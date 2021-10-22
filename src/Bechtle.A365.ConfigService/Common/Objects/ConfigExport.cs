using System;
using System.Linq;

namespace Bechtle.A365.ConfigService.Common.Objects
{
    /// <summary>
    ///     collection of exported parts of the configuration
    /// </summary>
    public record ConfigExport
    {
        /// <inheritdoc cref="EnvironmentExport" />
        public EnvironmentExport[] Environments { get; init; } = Array.Empty<EnvironmentExport>();

        /// <inheritdoc cref="LayerExport" />
        public LayerExport[] Layers { get; init; } = Array.Empty<LayerExport>();

        /// <inheritdoc />
        public virtual bool Equals(ConfigExport? other)
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
