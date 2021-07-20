using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bechtle.A365.ConfigService.Common.Serialization
{
    /// <summary>
    ///     Custom-Implementation of <see cref="JsonConverter{Double}"/> that writes <see cref="double.NaN"/> as "0"
    /// </summary>
    public class DoubleConverter : JsonConverter<double>
    {
        /// <inheritdoc />
        public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetDouble();
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(double.IsNaN(value) ? 0D : value);
        }
    }
}