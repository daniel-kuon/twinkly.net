using System.Text.Json;
using System.Text.Json.Serialization;

namespace Twinkly.Net;

public class LowercaseStringEnumConverter<T> : JsonConverter<T> where T : struct, Enum
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException();
        }

        // Convert the string to an enum, ignoring case
        if (Enum.TryParse(reader.GetString(), ignoreCase: true, out T value))
        {
            return value;
        }
        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        // Convert the enum to a string
        writer.WriteStringValue(value.ToString().ToLower());
    }
}

public class UpperCaseStringEnumConverter<T> : JsonConverter<T> where T : struct, Enum
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException();
        }

        // Convert the string to an enum, ignoring case
        if (Enum.TryParse(reader.GetString(), ignoreCase: true, out T value))
        {
            return value;
        }
        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        // Convert the enum to a string
        writer.WriteStringValue(value.ToString().ToUpper());
    }
}
