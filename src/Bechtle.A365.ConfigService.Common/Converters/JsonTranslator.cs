using System.Collections.Generic;
using System.Text.Json;

namespace Bechtle.A365.ConfigService.Common.Converters
{
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
        public JsonDocument ToJson(IDictionary<string, string> dict) => DictToJsonConverter.ToJson(dict, JsonTranslatorDefaultSettings.Separator);

        /// <inheritdoc />
        public JsonDocument ToJson(IDictionary<string, string> dict, string separator) => DictToJsonConverter.ToJson(dict, separator);
    }
}