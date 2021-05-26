using System;
using System.Collections.Generic;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     Named Layer of Keys, to be assigned to one or more Environments
    /// </summary>
    public sealed class EnvironmentLayer : DomainObject<LayerIdentifier>
    {
        /// <inheritdoc cref="LayerIdentifier" />
        public override LayerIdentifier Id { get; set; }

        /// <summary>
        ///     Json-Representation of the actual Data
        /// </summary>
        public string Json { get; set; }

        /// <summary>
        ///     Actual Data of this Layer
        /// </summary>
        public Dictionary<string, EnvironmentLayerKey> Keys { get; set; }

        /// <summary>
        ///     Keys represented as nested objects
        /// </summary>
        public List<EnvironmentLayerKeyPath> KeyPaths { get; set; }

        /// <inheritdoc />
        public EnvironmentLayer(LayerIdentifier identifier)
        {
            if (identifier is null)
            {
                throw new ArgumentNullException(nameof(identifier));
            }

            if (string.IsNullOrWhiteSpace(identifier.Name))
            {
                throw new ArgumentNullException(nameof(identifier), $"{nameof(identifier.Name)} is null or empty");
            }

            Id = identifier;
            Keys = new Dictionary<string, EnvironmentLayerKey>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
