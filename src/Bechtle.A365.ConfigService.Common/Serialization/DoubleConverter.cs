using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bechtle.A365.ConfigService.Common.Serialization
{
    public class DoubleConverter : JsonConverter<double>
    {
        public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetDouble();
        }

        public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(double.IsNaN(value) ? 0D : value);
        }
    }
}