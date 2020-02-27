using System;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     All Metadata used to represent a single Key inside a <see cref="ConfigEnvironment" />
    /// </summary>
    public class ConfigEnvironmentKey : IEquatable<ConfigEnvironmentKey>
    {
        /// <inheritdoc cref="ConfigEnvironmentKey" />
        public ConfigEnvironmentKey(string key, string value, string type, string description, long version)
        {
            Key = key;
            Value = value;
            Type = type;
            Description = description;
            Version = version;
        }

        /// <summary>
        ///     Description of the Contents of this Key
        /// </summary>
        public string Description { get; }

        /// <summary>
        ///     Full-Path for this Key
        /// </summary>
        public string Key { get; }

        /// <summary>
        ///     Intended Value-Type of this Key
        /// </summary>
        public string Type { get; }

        /// <summary>
        ///     String-Representation of the actual Value
        /// </summary>
        public string Value { get; }

        /// <summary>
        ///     internal data-version
        /// </summary>
        public long Version { get; }

        /// <inheritdoc />
        public virtual bool Equals(ConfigEnvironmentKey other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Description == other.Description
                   && Key == other.Key
                   && Type == other.Type
                   && Value == other.Value
                   && Version == other.Version;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ConfigEnvironmentKey) obj);
        }

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Description, Key, Type, Value, Version);

        /// <summary>
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator ==(ConfigEnvironmentKey left, ConfigEnvironmentKey right) => Equals(left, right);

        /// <summary>
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator !=(ConfigEnvironmentKey left, ConfigEnvironmentKey right) => !Equals(left, right);
    }
}