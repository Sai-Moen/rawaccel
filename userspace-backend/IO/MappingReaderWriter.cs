using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using userspace_backend.Data;

namespace userspace_backend.IO
{
    public class MappingReaderWriter : ReaderWriterBase<Mapping>
    {
        public static JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        protected override string FileType => "Mapping";

        public override string Serialize(Mapping toWrite)
        {
            return JsonSerializer.Serialize(toWrite, JsonOptions);
        }

        public override Mapping Deserialize(string toRead)
        {
            return JsonSerializer.Deserialize<Mapping>(toRead);
        }
    }
}
