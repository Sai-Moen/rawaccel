using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace userspace_backend.IO
{
    public abstract class ReaderWriterBase<T>
    {
        protected abstract string FileType { get; }

        public void Write(string path, T toWrite)
        {
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path))
            {
                throw new ArgumentException($"Not a valid path: [{path}]", nameof(path));
            }

            var parent = Directory.GetParent(path)?.FullName;

            if (!Directory.Exists(parent))
            {
                Directory.CreateDirectory(parent);
            }

            var devicesText = Serialize(toWrite);

            using (StreamWriter outputFile = new StreamWriter(path))
            {
                outputFile.Write(devicesText);
            }
        }
        public T Read(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(path);
            }

            T readIn = default;

            try
            {
                using (StreamReader fileToRead = new StreamReader(path))
                {
                    var fileText = fileToRead.ReadToEnd();

                    if (string.IsNullOrWhiteSpace(fileText))
                    {
                        throw new Exception($"{FileType} file is empty.");
                    }

                    readIn = Deserialize(fileText);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error parsing devices file at path {path}", ex);
            }

            return readIn;
        }

        public abstract string Serialize(T toWrite);

        public abstract T Deserialize(string toRead);
    }
}
