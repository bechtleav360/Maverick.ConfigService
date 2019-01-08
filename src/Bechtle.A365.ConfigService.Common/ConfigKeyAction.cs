namespace Bechtle.A365.ConfigService.Common
{
    /// <summary>
    ///     action to apply to a key
    /// </summary>
    public class ConfigKeyAction
    {
        /// <summary>
        ///     type of action to apply to this key
        /// </summary>
        public ConfigKeyActionType Type { get; }

        /// <summary>
        ///     key to which this action is applied
        /// </summary>
        public string Key { get; }

        /// <summary>
        ///     optional value 
        /// </summary>
        public string Value { get; }

        /// <summary>
        ///     short description of this key
        /// </summary>
        public string Description { get; }

        /// <summary>
        ///     intended type of this key
        /// </summary>
        public string ValueType { get; }

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

        /// <summary>
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static ConfigKeyAction Delete(string key) => new ConfigKeyAction(ConfigKeyActionType.Delete, key, null, null, null);
    }
}