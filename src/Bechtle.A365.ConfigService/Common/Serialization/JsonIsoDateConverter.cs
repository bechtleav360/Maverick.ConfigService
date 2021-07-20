using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bechtle.A365.ConfigService.Common.Serialization
{
    /// <summary>
    ///     Custom-Implementation of <see cref="JsonConverter{DateTime}"/> that writes Dates in ISO-8601 Format
    /// </summary>
    public class JsonIsoDateConverter : JsonConverter<DateTime>
    {
        /// <inheritdoc />
        public override DateTime Read(ref Utf8JsonReader reader,
                                      Type typeToConvert,
                                      JsonSerializerOptions options)
        {
            return reader.GetDateTime();
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer,
                                   DateTime value,
                                   JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("O"));
        }
    }
}