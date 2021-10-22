using System;

namespace Bechtle.A365.ConfigService.Common
{
    /// <summary>
    ///     action to apply to a key
    /// </summary>
    public class ConfigKeyAction : IEquatable<ConfigKeyAction>
    {
        /// <summary>
        ///     short description of this key
        /// </summary>
        public string Description { get; init; } = string.Empty;

        /// <summary>
        ///     key to which this action is applied
        /// </summary>
        public string Key { get; init; } = string.Empty;

        /// <summary>
        ///     type of action to apply to this key
        /// </summary>
        public ConfigKeyActionType Type { get; init; } = ConfigKeyActionType.Set;

        /// <summary>
        ///     optional value
        /// </summary>
        public string? Value { get; init; }

        /// <summary>
        ///     intended type of this key
        /// </summary>
        public string ValueType { get; init; } = string.Empty;

        // ReSharper disable once UnusedMember.Global
        /// <summary>
        ///     DO NOT USE - Constructors for this Type should only be used to De-/Serialize instances of this Class
        /// </summary>
        public ConfigKeyAction()
        {
        }

        // ReSharper disable once UnusedMember.Global
        /// <summary>
        ///     DO NOT USE - Constructors for this Type should only be used to De-/Serialize instances of this Class
        /// </summary>
        public ConfigKeyAction(ConfigKeyActionType type, string key, string? value, string? description, string? valueType)
        {
            Type = type;
            Key = key;
            Value = value;
            Description = description ?? string.Empty;
            ValueType = valueType ?? string.Empty;
        }

        /// <inheritdoc />
        public virtual bool Equals(ConfigKeyAction? other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return string.Equals(Description, other.Description, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(Key, other.Key, StringComparison.OrdinalIgnoreCase)
                   && Type == other.Type
                   && string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(ValueType, other.ValueType, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static ConfigKeyAction Delete(string key) => new(ConfigKeyActionType.Delete, key, null, null, null);

        /// <inheritdoc />
        public override bool Equals(object? obj)
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

            return Equals((ConfigKeyAction)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Description.GetHashCode();
                hashCode = (hashCode * 397) ^ Key.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)Type;
                hashCode = (hashCode * 397) ^ (Value != null ? Value.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ ValueType.GetHashCode();
                return hashCode;
            }
        }

        /// <inheritdoc cref="operator ==" />
        public static bool operator ==(ConfigKeyAction? left, ConfigKeyAction? right) => Equals(left, right);

        /// <inheritdoc cref="operator !=" />
        public static bool operator !=(ConfigKeyAction? left, ConfigKeyAction? right) => !Equals(left, right);

        /// <summary>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static ConfigKeyAction Set(string key, string? value) => Set(key, value, null, null);

        /// <summary>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="description"></param>
        /// <param name="valueType"></param>
        /// <returns></returns>
        public static ConfigKeyAction Set(string key, string? value, string? description, string? valueType)
            => new(ConfigKeyActionType.Set, key, value, description, valueType);
    }
}
