using System.Text.Json;
using System.Text.Json.Serialization;

namespace TTXEquipamentos.Converters
{
    /// <summary>
    /// Case-insensitive JSON converter for enums
    /// Allows "corretiva" or "CORRETIVA" to deserialize to MaintenanceType.corretiva
    /// </summary>
    public class CaseInsensitiveEnumConverter<T> : JsonConverter<T> where T : struct, Enum
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException($"Unexpected token {reader.TokenType} when parsing enum");
            }

            var value = reader.GetString();
            if (string.IsNullOrEmpty(value))
            {
                throw new JsonException("Empty enum value");
            }

            // Try to find the enum value case-insensitively
            foreach (var name in Enum.GetNames(typeToConvert))
            {
                if (string.Equals(name, value, StringComparison.OrdinalIgnoreCase))
                {
                    return Enum.Parse<T>(name);
                }
            }

            throw new JsonException($"Unknown enum value '{value}' for type {typeToConvert.Name}");
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString().ToLower());
        }
    }
}
