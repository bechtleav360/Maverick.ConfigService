using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Bechtle.A365.ConfigService.Common.Converters
{
    public class JsonTranslator : IJsonTranslator
    {
        private readonly DictToJsonConverter _dictToJsonConverter;
        private readonly JsonToDictConverter _jsonToDictConverter;

        /// <inheritdoc />
        public JsonTranslator()
        {
            _dictToJsonConverter = new DictToJsonConverter();
            _jsonToDictConverter = new JsonToDictConverter();
        }

        /// <inheritdoc />
        public IDictionary<string, string> ToDictionary(JToken json)
            => ToDictionary(json, JsonTranslatorDefaultSettings.Separator, JsonTranslatorDefaultSettings.EscapePath);

        /// <inheritdoc />
        public IDictionary<string, string> ToDictionary(JToken json, bool encodePath)
            => ToDictionary(json, JsonTranslatorDefaultSettings.Separator, encodePath);

        /// <inheritdoc />
        public IDictionary<string, string> ToDictionary(JToken json, string separator)
            => ToDictionary(json, separator, JsonTranslatorDefaultSettings.EscapePath);

        /// <inheritdoc />
        public IDictionary<string, string> ToDictionary(JToken json, string separator, bool encodePath)
            => _jsonToDictConverter.ToDict(json, separator, encodePath);

        /// <inheritdoc />
        public JToken ToJson(IDictionary<string, string> dict) => _dictToJsonConverter.ToJson(dict, JsonTranslatorDefaultSettings.Separator);

        /// <inheritdoc />
        public JToken ToJson(IDictionary<string, string> dict, string separator) => _dictToJsonConverter.ToJson(dict, separator);
    }
}