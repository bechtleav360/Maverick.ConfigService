namespace Bechtle.A365.ConfigService.Dto
{
    /// <summary>
    ///     action to apply to a key
    /// </summary>
    public class ConfigKeyAction
    {
        /// <summary>
        ///     type of action to apply to this key
        /// </summary>
        public ConfigKeyActionType Type { get; set; }

        /// <summary>
        ///     key to which this action is applied
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        ///     optional value 
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// </summary>
        /// <param name="type"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public ConfigKeyAction(ConfigKeyActionType type, string key, string value)
        {
            Type = type;
            Key = key;
            Value = value;
        }

        /// <summary>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static ConfigKeyAction Set(string key, string value) => new ConfigKeyAction(ConfigKeyActionType.Set, key, value);

        /// <summary>
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static ConfigKeyAction Delete(string key) => new ConfigKeyAction(ConfigKeyActionType.Delete, key, null);
    }
}