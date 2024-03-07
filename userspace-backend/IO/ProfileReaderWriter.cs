using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using userspace_backend.Data;

namespace userspace_backend.IO
{
    public class ProfileReaderWriter : ReaderWriterBase<Profile>
    {
        public static JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
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
