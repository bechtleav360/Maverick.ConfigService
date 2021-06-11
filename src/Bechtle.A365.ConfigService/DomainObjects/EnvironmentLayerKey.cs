using System;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     All Metadata used to represent a single Key inside a <see cref="EnvironmentLayer" />
    /// </summary>
    public class EnvironmentLayerKey : IEquatable<EnvironmentLayerKey>
    {
        /// <summary>
        ///     Description of the Contents of this Key
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        ///     Full-Path for this Key
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        ///     Intended Value-Type of this Key
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        ///     String-Representation of the actual Value
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        ///     internal data-version
        /// </summary>
        public long Version { get; set; }

        /// <inheritdoc cref="EnvironmentLayerKey" />
        public EnvironmentLayerKey()
        {
            Key = string.Empty;
            Value = string.Empty;
            Type = string.Empty;
            Description = string.Empty;
            Version = 0;
        }

        /// <inheritdoc cref="EnvironmentLayerKey" />
        public EnvironmentLayerKey(string key, string value, string type, string description, long version)
        {
            Key = key;
            Value = value;
            Type = type;
            Description = description;
            Version = version;
        }

        /// <inheritdoc />
        public virtual bool Equals(EnvironmentLayerKey other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Description == other.Description
                   && Key == other.Key
                   && Type == other.Type
                   && Value == other.Value
                   && Version == other.Version;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((EnvironmentLayerKey) obj);
        }

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Description, Key, Type, Value, Version);

        /// <summary>
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator ==(EnvironmentLayerKey left, EnvironmentLayerKey right) => Equals(left, right);

        /// <summary>
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator !=(EnvironmentLayerKey left, EnvironmentLayerKey right) => !Equals(left, right);
    }
}
