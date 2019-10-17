using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bechtle.A365.ConfigService.Common.Serialization
{
    public class FloatConverter : JsonConverter<float>
    {
        public override float Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetSingle();
        }

        public override void Write(Utf8JsonWriter writer, float value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(float.IsNaN(value) ? 0F : value);
        }
    }
}