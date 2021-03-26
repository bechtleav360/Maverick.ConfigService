﻿using System;
using System.Collections.Generic;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <summary>
    ///     Layer->Environment assignment was updated
    /// </summary>
    public class EnvironmentLayersModified : DomainEvent, IEquatable<EnvironmentLayersModified>
    {
        /// <inheritdoc />
        public EnvironmentLayersModified(EnvironmentIdentifier identifier, List<LayerIdentifier> layers)
        {
            Identifier = identifier;
            Layers = new List<LayerIdentifier>(layers);
        }

        /// <inheritdoc cref="EnvironmentIdentifier" />
        public EnvironmentIdentifier Identifier { get; set; }

        /// <summary>
        ///     List of Layers used in this Environment.
        ///     Their indices equal their order.
        ///     <list type="bullet">
        ///         <item>0. {base-layer}</item>
        ///         <item>n. {overwrites 0..n-1}</item>
        ///     </list>
        /// </summary>
        public List<LayerIdentifier> Layers { get; set; }

        public static bool operator ==(EnvironmentLayersModified left, EnvironmentLayersModified right) => Equals(left, right);

        public static bool operator !=(EnvironmentLayersModified left, EnvironmentLayersModified right) => !Equals(left, right);

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((EnvironmentLayersModified) obj);
        }

        /// <inheritdoc />
        public override bool Equals(DomainEvent other, bool strict) => Equals(other as EnvironmentLayersModified, strict);

        public virtual bool Equals(EnvironmentLayersModified other) => Equals(other, false);

        public virtual bool Equals(EnvironmentLayersModified other, bool _)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Identifier, other.Identifier)
                   && Equals(Layers, other.Layers);
        }

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Identifier, Layers);

        /// <inheritdoc />
        public override DomainEventMetadata GetMetadata() => new DomainEventMetadata
        {
            Filters =
            {
                {KnownDomainEventMetadata.Identifier, Identifier.ToString()}
            }
        };
    }
}