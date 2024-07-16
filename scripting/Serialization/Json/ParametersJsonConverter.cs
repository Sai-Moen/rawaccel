using scripting.Script;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace scripting.Serialization.Json;

public class ParametersJsonConverter(Parameters old) : JsonConverter<Parameters>
{
    private const string nameText = "parameters";

    private readonly JsonConverter<Parameter> parameterConverter = new ParameterJsonConverter(old);

    public override Parameters? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        int startDepth = reader.CurrentDepth;

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException($"Invalid start for a Parameters object: {reader.TokenType}");
        }

        Parameters parameters = [];
        while (reader.Read() && (reader.TokenType != JsonTokenType.EndObject || reader.CurrentDepth != startDepth))
        {
            var p = parameterConverter.Read(ref reader, typeToConvert, options)
                ?? throw new JsonException("Parameter cannot be null!");
            parameters.Add(p);
        }
        return parameters;
    }

    public override void Write(Utf8JsonWriter writer, Parameters value, JsonSerializerOptions options)
    {
        writer.WriteStartObject(nameText);
        foreach (Parameter p in value)
        {
            parameterConverter.Write(writer, p, options);
        }
        writer.WriteEndObject();
    }
}
