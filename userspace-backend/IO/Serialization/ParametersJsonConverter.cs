using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using userspace_backend.ScriptingLanguage.Script;

namespace userspace_backend.IO.Serialization;

public class ParametersJsonConverter(Parameters old) : JsonConverter<Parameters>
{
    public override Parameters? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        int startDepth = reader.CurrentDepth;

        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException($"Invalid start for a Parameters object: {reader.TokenType}");
        }

        Parameters parameters = [];
        while (reader.Read() && (reader.TokenType != JsonTokenType.EndArray || reader.CurrentDepth != startDepth))
        {
            int index = parameters.Count;
            if (index >= old.Count)
            {
                throw new JsonException("Settings have more parameters than expected!");
            }

            ParameterJsonConverter converter = new(old[index]);
            var p = converter.Read(ref reader, typeToConvert, options)
                ?? throw new JsonException("Parameter cannot be null!");

            parameters.Add(p);
        }

        if (parameters.Count < old.Count)
        {
            throw new JsonException("Settings have fewer parameters than expected!");
        }

        return parameters;
    }

    public override void Write(Utf8JsonWriter writer, Parameters value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();

        foreach (Parameter p in value)
        {
            ParameterJsonConverter converter = new(p);
            converter.Write(writer, p, options);
        }

        writer.WriteEndArray();
    }
}
