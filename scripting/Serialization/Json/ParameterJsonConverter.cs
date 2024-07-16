using scripting.Script;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace scripting.Serialization.Json;

internal class ParameterJsonConverter(Parameters old) : JsonConverter<Parameter>
{
    private const string typeText = "type";
    private const string valueText = "value";

    public override Parameter? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        int startDepth = reader.CurrentDepth;

        string name = reader.GetString()
            ?? throw new JsonException("Parameter name cannot be null!");
        if (reader.TokenType != JsonTokenType.PropertyName)
        {
            throw new JsonException($"Bad property name type for a Parameter object: {reader.TokenType}");
        }
        
        if (!old.TryFindByName(name, out var p))
        {
            throw new JsonException($"Could not find Parameter object with name: {name}");
        }

        if (reader.Read() && reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Could not find Parameter object start!");
        }

        ParameterType type;
        if (CheckPropertyName(ref reader, typeText))
        {
            if (!Enum.TryParse(reader.GetString(), out type))
            {
                throw new JsonException("Could not parse Parameter type!");
            }
        }
        else
        {
            throw new JsonException("Bad property name for Parameter type!");
        }

        Number value;
        if (CheckPropertyName(ref reader, valueText))
        {
            value = reader.GetDouble();
        }
        else
        {
            throw new JsonException("Bad property name for Parameter value!");
        }

        if (reader.Read() && (reader.TokenType != JsonTokenType.EndObject || reader.CurrentDepth != startDepth))
        {
            throw new JsonException("Could not find Parameter object end!");
        }

        return new(p, type, value);
    }

    private static bool CheckPropertyName(ref Utf8JsonReader reader, string expectedText)
    {
        var name = reader.GetString();
        if (string.IsNullOrEmpty(name)) return false;
        else return reader.TokenType == JsonTokenType.PropertyName && name == expectedText;
    }

    public override void Write(Utf8JsonWriter writer, Parameter value, JsonSerializerOptions options)
    {
        writer.WriteStartObject(value.Name);
            writer.WriteString(typeText, Enum.GetName(value.Type));
            writer.WriteNumber(valueText, value.Value);
        writer.WriteEndObject();
    }
}