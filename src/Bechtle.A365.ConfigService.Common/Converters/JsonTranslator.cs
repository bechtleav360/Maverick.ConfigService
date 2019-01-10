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
        public IDictionary<string, string> ToDictionary(JToken json) => _jsonToDictConverter.ToDict(json);

        /// <inheritdoc />
        public JToken ToJson(IDictionary<string, string> dict) => _dictToJsonConverter.ToJson(dict);
    }
}