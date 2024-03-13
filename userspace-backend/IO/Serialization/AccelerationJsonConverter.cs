using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using userspace_backend.Data.Profiles;
using userspace_backend.Data.Profiles.Accel;
using userspace_backend.Data.Profiles.Accel.Formula;
using static userspace_backend.Data.Profiles.Accel.FormulaAccel;
using static userspace_backend.Data.Profiles.Acceleration;

namespace userspace_backend.IO.Serialization
{
    public class AccelerationJsonConverter : JsonConverter<Acceleration>
    {
        public override Acceleration? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Utf8JsonReader typeReader = reader;
            int startDepth = reader.CurrentDepth;

            string typeString = ReadToType(ref reader);
            ReadToEnd(ref reader, startDepth);

            string[] typeStringSplit = typeString.Split('/');
            string baseType = typeStringSplit[0];
            AccelerationDefinitionType definitionType = DetermineDefinitionType(baseType);

            return CreateAccelerationOfType(definitionType, typeStringSplit, ref typeReader);
        }

        private static string ReadToType(ref Utf8JsonReader reader)
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName
                    && reader.GetString() == "Type")
                {
                    break;
                }
            }

            if (!reader.Read())
            {
                throw new JsonException("Acceleration definition must include \"Type\".");
            }

            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("Type must be a string.");
            }

            string typeString = reader.GetString();

            if (string.IsNullOrEmpty(typeString))
            {
                throw new JsonException("\"Type\" must have a non-empty and non-null value.");
            }

            return typeString;
        }

        private static void ReadToEnd(ref Utf8JsonReader reader, int depth)
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == depth)
                {
                    return;
                }
            }
        }

        private static AccelerationDefinitionType DetermineDefinitionType(string typeStringFromJson)
        {
            if (!Enum.TryParse(typeStringFromJson, ignoreCase: true, out AccelerationDefinitionType result))
            {
                throw new JsonException($"Acceleration base type [\"{typeStringFromJson}\"] not valid." +
                    $"Valid values: [{string.Join(", ", Enum.GetNames(typeof(AccelerationDefinitionType)))}");
            }

            return result;
        }

        private static Acceleration CreateAccelerationOfType(AccelerationDefinitionType defnType, string[] defnSplit, ref Utf8JsonReader readerFromStart)
        {
            switch (defnType)
            {
                case AccelerationDefinitionType.Formula:
                    return CreateFormulaAccel(defnSplit, ref readerFromStart);
                case AccelerationDefinitionType.None:
                default:
                    return new NoAcceleration();
            }
        }

        private static FormulaAccel CreateFormulaAccel(string[] defnSplit, ref Utf8JsonReader readerFromStart)
        {
            if (defnSplit.Length < 1)
            {
                throw new JsonException("Type \"Formula\" must be followed by a forward slash and formula type. Example: \"Type\": \"Formula/Classic\"");
            }

            string formulaTypeString = defnSplit[1];

            if (string.IsNullOrEmpty(formulaTypeString))
            {
                throw new JsonException("Formula/[Type] must have a non-empty and non-null value.");
            }

            AccelFormulaType formulaType = DetermineFormulaType(formulaTypeString);

            switch (formulaType)
            {
                case AccelFormulaType.Linear:
                    return JsonSerializer.Deserialize<LinearAccel>(ref readerFromStart);
                case AccelFormulaType.Classic:
                    return JsonSerializer.Deserialize<ClassicAccel>(ref readerFromStart);
                default:
                    throw new JsonException($"Unknown formula type {formulaTypeString}");
            }
        }

        private static AccelFormulaType DetermineFormulaType(string formulaTypeFromJson)
        {
            if (!Enum.TryParse(formulaTypeFromJson, ignoreCase: true, out AccelFormulaType result))
            {
                throw new JsonException($"Acceleration formula type [\"{formulaTypeFromJson}\"] not valid." +
                    $"Valid values: [{string.Join(", ", Enum.GetNames(typeof(AccelFormulaType)))}");
            }

            return result;
        }

        public override void Write(Utf8JsonWriter writer, Acceleration value, JsonSerializerOptions options)
        {
        }

    }
}
