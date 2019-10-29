using System;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <summary>
    ///     Information to identify an Environment
    /// </summary>
    public class EnvironmentIdentifier : Identifier, IEquatable<EnvironmentIdentifier>
    {
        /// <inheritdoc />
        public EnvironmentIdentifier(string category, string name)
        {
            Category = category;
            Name = name;
        }

        /// <summary>
        ///     Category for a group of Environments, think Folder / Tenant and the like
        /// </summary>
        public string Category { get; }

        /// <summary>
        ///     Unique name for an Environment within a <see cref="Category" />
        /// </summary>
        public string Name { get; }

        public bool Equals(EnvironmentIdentifier other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Category, other.Category, StringComparison.OrdinalIgnoreCase) && string.Equals(Name, other.Name);
        }

        public static bool operator ==(EnvironmentIdentifier left, EnvironmentIdentifier right) => Equals(left, right);

        public static bool operator !=(EnvironmentIdentifier left, EnvironmentIdentifier right) => !Equals(left, right);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((EnvironmentIdentifier) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Category != null ? Category.GetHashCode() : 0) * 397) ^ (Name != null ? Name.GetHashCode() : 0);
            }
        }

        /// <inheritdoc />
        public override string ToString() => $"[{nameof(EnvironmentIdentifier)}; {nameof(Category)}: '{Category}'; {nameof(Name)}: '{Name}']";
    }
}