using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using userspace_backend.ScriptingLanguage.Script;

namespace userspace_backend.IO.Serialization;

internal class ParameterJsonConverter(Parameter old) : JsonConverter<Parameter>
{
    private const string nameText = "name";
    private const string typeText = "type";
    private const string valueText = "value";

    public override Parameter? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        int startDepth = reader.CurrentDepth;

        CheckPropertyName(ref reader, nameText);
        string name = reader.GetString()
            ?? throw new JsonException("Parameter name cannot be null!");

        if (name != old.Name)
        {
            throw new JsonException($"Wrong Parameter name; expected: {old.Name}, got {name} instead!");
        }

        CheckPropertyName(ref reader, typeText);
        if (!Enum.TryParse(reader.GetString(), out ParameterType type))
        {
            throw new JsonException("Could not parse Parameter type!");
        }

        Number value;
        CheckPropertyName(ref reader, valueText);
        value = reader.GetDouble();

        if (reader.Read() && (reader.TokenType != JsonTokenType.EndObject || reader.CurrentDepth != startDepth))
        {
            throw new JsonException("Could not find Parameter object end!");
        }

        return new(old, type, value);
    }

    private static void CheckPropertyName(ref Utf8JsonReader reader, string expectedText)
    {
        var propertyName = reader.GetString();
        if (reader.TokenType != JsonTokenType.PropertyName)
        {
            throw new JsonException("Expected Property Name!");
        }
        else if (propertyName != expectedText)
        {
            throw new JsonException($"Bad property name for Parameter; expected {expectedText}, got {propertyName} instead!");
        }
    }

    public override void Write(Utf8JsonWriter writer, Parameter value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteString(nameText, value.Name);
        writer.WriteString(typeText, Enum.GetName(value.Type));
        writer.WriteNumber(valueText, value.Value);

        writer.WriteEndObject();
    }
}