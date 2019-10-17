﻿using System.Collections.Generic;
using System.Text.Json;

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
        public IDictionary<string, string> ToDictionaryNative(JsonElement json)
            => ToDictionaryNative(json, JsonTranslatorDefaultSettings.Separator, JsonTranslatorDefaultSettings.EscapePath);

        /// <inheritdoc />
        public IDictionary<string, string> ToDictionaryNative(JsonElement json, bool encodePath)
            => ToDictionaryNative(json, JsonTranslatorDefaultSettings.Separator, encodePath);

        /// <inheritdoc />
        public IDictionary<string, string> ToDictionaryNative(JsonElement json, string separator)
            => ToDictionaryNative(json, separator, JsonTranslatorDefaultSettings.EscapePath);

        /// <inheritdoc />
        public IDictionary<string, string> ToDictionaryNative(JsonElement json, string separator, bool encodePath)
            => _jsonToDictConverter.ToDictNative(json, separator, encodePath);

        /// <inheritdoc />
        public string ToJsonNative(IDictionary<string, string> dict) => _dictToJsonConverter.ToJsonNative(dict, JsonTranslatorDefaultSettings.Separator);

        /// <inheritdoc />
        public string ToJsonNative(IDictionary<string, string> dict, string separator) => _dictToJsonConverter.ToJsonNative(dict, separator);
    }
}