using System;

namespace Bechtle.A365.ConfigService.Common
{
    /// <summary>
    ///     action to apply to a key
    /// </summary>
    public class ConfigKeyAction : IEquatable<ConfigKeyAction>
    {
        /// <summary>
        /// </summary>
        /// <param name="type"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="description"></param>
        /// <param name="valueType"></param>
        // ReSharper disable once MemberCanBePrivate.Global
        // needs to be constructable by Newtonsoft.Json
        public ConfigKeyAction(ConfigKeyActionType type, string key, string value, string description, string valueType)
        {
            Type = type;
            Key = key;
            Value = value;
            Description = description;
            ValueType = valueType;
        }

        /// <summary>
        ///     short description of this key
        /// </summary>
        public string Description { get; }

        /// <summary>
        ///     key to which this action is applied
        /// </summary>
        public string Key { get; }

        /// <summary>
        ///     type of action to apply to this key
        /// </summary>
        public ConfigKeyActionType Type { get; }

        /// <summary>
        ///     optional value
        /// </summary>
        public string Value { get; }

        /// <summary>
        ///     intended type of this key
        /// </summary>
        public string ValueType { get; }

        /// <summary>
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static ConfigKeyAction Delete(string key) => new ConfigKeyAction(ConfigKeyActionType.Delete, key, null, null, null);

        /// <inheritdoc cref="operator ==" />
        public static bool operator ==(ConfigKeyAction left, ConfigKeyAction right) => Equals(left, right);

        /// <inheritdoc cref="operator !=" />
        public static bool operator !=(ConfigKeyAction left, ConfigKeyAction right) => !Equals(left, right);

        /// <summary>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static ConfigKeyAction Set(string key, string value) => Set(key, value, null, null);

        /// <summary>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="description"></param>
        /// <param name="valueType"></param>
        /// <returns></returns>
        public static ConfigKeyAction Set(string key, string value, string description, string valueType)
            => new ConfigKeyAction(ConfigKeyActionType.Set, key, value, description, valueType);

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ConfigKeyAction) obj);
        }

        /// <inheritdoc />
        public virtual bool Equals(ConfigKeyAction other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Description, other.Description, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(Key, other.Key, StringComparison.OrdinalIgnoreCase)
                   && Type == other.Type
                   && string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(ValueType, other.ValueType, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Description != null ? Description.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (Key != null ? Key.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int) Type;
                hashCode = (hashCode * 397) ^ (Value != null ? Value.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ValueType != null ? ValueType.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}