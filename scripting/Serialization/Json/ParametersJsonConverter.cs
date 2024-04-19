using scripting.Script;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace scripting.Serialization.Json;

public class ParametersJsonConverter(Parameters old) : JsonConverter<Parameters>
{
    private static readonly JsonEncodedText nameText = JsonEncodedText.Encode("parameters");

    private readonly JsonConverter<Parameter> parameterConverter = new ParameterJsonConverter(old);

    public override Parameters? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.StartObject:
                break;
            default:
                throw new JsonException($"Invalid start for a Parameters object: {reader.TokenType}");
        }

        int startDepth = reader.CurrentDepth;

        Parameters parameters = [];
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == startDepth)
            {
                break;
            }

            var p = parameterConverter.Read(ref reader, typeToConvert, options)
                ?? throw new JsonException("Parameter cannot be null!");
            parameters.Add(p);
        }
        return parameters;
    }

    public override void Write(Utf8JsonWriter writer, Parameters value, JsonSerializerOptions options)
    {
        if (value.Count == 0)
        {
            writer.WriteNull(nameText);
            return;
        }

        writer.WriteStartObject(nameText);
        foreach (Parameter p in value)
        {
            parameterConverter.Write(writer, p, options);
        }
        writer.WriteEndObject();
    }
}
