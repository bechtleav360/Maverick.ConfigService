using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bechtle.A365.ConfigService.Common.Serialization
{
    public class JsonIsoDateConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader,
                                      Type typeToConvert,
                                      JsonSerializerOptions options)
        {
            return reader.GetDateTime();
        }

        public override void Write(Utf8JsonWriter writer,
                                   DateTime value,
                                   JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("O"));
        }
    }
}