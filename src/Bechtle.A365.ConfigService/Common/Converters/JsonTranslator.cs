using System.Collections.Generic;
using System.Text.Json;

namespace Bechtle.A365.ConfigService.Common.Converters
{
    /// <summary>
    ///     Implementation of <see cref="IJsonTranslator"/> that forwards calls to <see cref="DictToJsonConverter"/> and <see cref="JsonToDictConverter"/>
    /// </summary>
    public class JsonTranslator : IJsonTranslator
    {
        /// <inheritdoc />
        public IDictionary<string, string> ToDictionary(JsonElement json)
            => ToDictionary(json, JsonTranslatorDefaultSettings.Separator, JsonTranslatorDefaultSettings.EscapePath);

        /// <inheritdoc />
        public IDictionary<string, string> ToDictionary(JsonElement json, bool encodePath)
            => ToDictionary(json, JsonTranslatorDefaultSettings.Separator, encodePath);

        /// <inheritdoc />
        public IDictionary<string, string> ToDictionary(JsonElement json, string separator)
            => ToDictionary(json, separator, JsonTranslatorDefaultSettings.EscapePath);

        /// <inheritdoc />
        public IDictionary<string, string> ToDictionary(JsonElement json, string separator, bool encodePath)
            => JsonToDictConverter.ToDict(json, separator, encodePath);

        /// <inheritdoc />
        public JsonElement ToJson(IDictionary<string, string> dict) => DictToJsonConverter.ToJson(dict, JsonTranslatorDefaultSettings.Separator);

        /// <inheritdoc />
        public JsonElement ToJson(IDictionary<string, string> dict, string separator) => DictToJsonConverter.ToJson(dict, separator);
    }
}