using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace App.Commons.JsonConverters
{
    public class Decimal4JsonConverter : JsonConverter<decimal>
    {
        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number && reader.TryGetDecimal(out var d)) return d;
            if (reader.TokenType == JsonTokenType.String && decimal.TryParse(reader.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var s)) return s;
            return 0m;
        }

        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
        {
            // Write numeric literal with 4 decimal places
            writer.WriteRawValue(value.ToString("0.0000", CultureInfo.InvariantCulture));
        }
    }

    public class NullableDecimal4JsonConverter : JsonConverter<decimal?>
    {
        public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            if (reader.TokenType == JsonTokenType.Number && reader.TryGetDecimal(out var d)) return d;
            if (reader.TokenType == JsonTokenType.String && decimal.TryParse(reader.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var s)) return s;
            return null;
        }

        public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
        {
            if (!value.HasValue) writer.WriteNullValue();
            else writer.WriteRawValue(value.Value.ToString("0.0000", CultureInfo.InvariantCulture));
        }
    }
}
