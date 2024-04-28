using System.Text.Json;
using System.Text.Json.Serialization;
using userspace_backend.Data;

namespace userspace_backend.IO
{
    public class ProfileReaderWriter : ReaderWriterBase<Profile>
    {
        public static JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        protected override string FileType => "Profile";

        public override Profile Deserialize(string toRead)
        {
            return JsonSerializer.Deserialize<Profile>(toRead);
        }

        public override string Serialize(Profile toWrite)
        {
            return JsonSerializer.Serialize(toWrite, JsonOptions);
        }
    }
}
