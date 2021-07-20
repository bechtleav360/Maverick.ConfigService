using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bechtle.A365.ConfigService.Common.Serialization
{
    /// <summary>
    ///     Custom-Implementation of <see cref="JsonConverter{Float}"/> that writes <see cref="float.NaN"/> as "0" 
    /// </summary>
    public class FloatConverter : JsonConverter<float>
    {
        /// <inheritdoc />
        public override float Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetSingle();
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, float value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(float.IsNaN(value) ? 0F : value);
        }
    }
}