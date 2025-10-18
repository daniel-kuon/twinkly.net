using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;
using System.Collections.Concurrent;

namespace Twinkly.Net;

[AttributeUsage(AttributeTargets.Field)]
public class JsonValueAttribute(string value) : Attribute
{
    public string Value { get; } = value;
}

public enum Case
{
    Upper,
    Lower
}

[AttributeUsage(AttributeTargets.Enum)]
public class EnumCaseAttribute(Case @case) : Attribute
{
    public Case Case { get; } = @case;
}

public class StringEnumConverter<T> : JsonConverter<T> where T : struct, Enum
{
    private static readonly ConcurrentDictionary<string, T> ReadCache = BuildReadCache();
    private static readonly ConcurrentDictionary<T, string> WriteCache = BuildWriteCache();

    private static ConcurrentDictionary<string, T> BuildReadCache()
    {
        var cache = new ConcurrentDictionary<string, T>();
        var enumAttr = typeof(T).GetCustomAttribute<EnumCaseAttribute>();
        foreach (var field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var attr = field.GetCustomAttribute<JsonValueAttribute>();
            if (attr != null)
            {
                cache[attr.Value] = (T)field.GetValue(null)!;
            }
            var name = field.Name;
            if (enumAttr != null)
            {
                name = enumAttr.Case == Case.Upper ? name.ToUpper() : name.ToLower();
            }
            else
            {
                name = name.ToLower();
            }
            cache[name] = (T)field.GetValue(null)!;
        }
        return cache;
    }

    private static ConcurrentDictionary<T, string> BuildWriteCache()
    {
        var cache = new ConcurrentDictionary<T, string>();
        var enumAttr = typeof(T).GetCustomAttribute<EnumCaseAttribute>();
        foreach (var field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var value = (T)field.GetValue(null)!;
            var attr = field.GetCustomAttribute<JsonValueAttribute>();
            if (attr != null)
            {
                cache[value] = attr.Value;
            }
            else
            {
                var str = field.Name;
                if (enumAttr != null)
                {
                    str = enumAttr.Case == Case.Upper ? str.ToUpper() : str.ToLower();
                }
                else
                {
                    str = str.ToLower();
                }
                cache[value] = str;
            }
        }
        return cache;
    }

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected string token for enum {typeof(T).Name}, but got {reader.TokenType}.");
        }

        string str = reader.GetString() ?? string.Empty;
        if (ReadCache.TryGetValue(str, out var result))
        {
            return result;
        }
        throw new JsonException($"Invalid value '{str}' for enum {typeof(T).Name}. Expected one of: {string.Join(", ", ReadCache.Keys)}");
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        if (WriteCache.TryGetValue(value, out var str))
        {
            writer.WriteStringValue(str);
        }
        else
        {
            throw new JsonException($"Unexpected enum value {value} for {typeof(T).Name}. This should not happen.");
        }
    }
}

public class EnumStringConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsEnum;

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(StringEnumConverter<>).MakeGenericType(typeToConvert);
        return Activator.CreateInstance(converterType) as JsonConverter ?? throw new InvalidOperationException($"Could not create converter for {typeToConvert.Name}");
    }
}
