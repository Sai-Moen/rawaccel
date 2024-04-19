using scripting.Script;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace scripting.Serialization.Json;

internal class ParameterJsonConverter(Parameters old) : JsonConverter<Parameter>
{
    private static readonly JsonEncodedText valueText = JsonEncodedText.Encode("value");
    private static readonly JsonEncodedText typeText = JsonEncodedText.Encode("type");

    public override Parameter? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        int startDepth = reader.CurrentDepth;

        string name = reader.GetString()
            ?? throw new JsonException("Parameter name cannot be null!");
        if (!old.TryFindByName(name, out var p))
        {
            throw new JsonException($"Could not find Parameter object with name: {name}");
        }

        if (reader.TokenType != JsonTokenType.PropertyName)
        {
            throw new JsonException($"Bad property name type for a Parameter object: {reader.TokenType}");
        }

        if (reader.Read() && reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Could not find Parameter object start!");
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

        if (reader.Read() && (reader.TokenType != JsonTokenType.EndObject || reader.CurrentDepth != startDepth))
        {
            throw new JsonException("Could not find Parameter object end!");
        }

        return new(p, value, type);
    }

    private static bool CheckPropertyName(ref Utf8JsonReader reader, JsonEncodedText expectedText)
    {
        var key = reader.GetString();
        if (string.IsNullOrEmpty(key))
        {
            return false;
        }
        
        return reader.TokenType == JsonTokenType.PropertyName && key == expectedText.Value;
    }

    public override void Write(Utf8JsonWriter writer, Parameter value, JsonSerializerOptions options)
    {
        writer.WriteStartObject(value.Name);
            writer.WriteNumber(valueText, value.Value);
            writer.WriteString(typeText, Enum.GetName(value.Type));
        writer.WriteEndObject();
    }
}