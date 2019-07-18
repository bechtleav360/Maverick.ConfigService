﻿using System;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <inheritdoc cref="DomainEvent" />
    /// <summary>
    ///     an environment with accompanying keys has been imported
    /// </summary>
    public class EnvironmentKeysImported : DomainEvent, IEquatable<EnvironmentKeysImported>
    {
        /// <inheritdoc />
        public EnvironmentKeysImported(EnvironmentIdentifier identifier, ConfigKeyAction[] modifiedKeys)
        {
            Identifier = identifier;
            ModifiedKeys = modifiedKeys;
        }

        /// <inheritdoc />
        public EnvironmentKeysImported()
        {
        }

        /// <inheritdoc cref="EnvironmentIdentifier" />
        public EnvironmentIdentifier Identifier { get; set; }

        /// <summary>
        ///     list of Actions that have been applied to the keys
        /// </summary>
        public ConfigKeyAction[] ModifiedKeys { get; set; }

        public bool Equals(EnvironmentKeysImported other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Identifier, other.Identifier) && Equals(ModifiedKeys, other.ModifiedKeys);
        }

        public static bool operator ==(EnvironmentKeysImported left, EnvironmentKeysImported right) => Equals(left, right);

        public static bool operator !=(EnvironmentKeysImported left, EnvironmentKeysImported right) => !Equals(left, right);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((EnvironmentKeysImported) obj);
        }

        public override bool Equals(DomainEvent other) => Equals(other as EnvironmentKeysImported);

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Identifier != null ? Identifier.GetHashCode() : 0) * 397) ^ (ModifiedKeys != null ? ModifiedKeys.GetHashCode() : 0);
            }
        }
    }
}