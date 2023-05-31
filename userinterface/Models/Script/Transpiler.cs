using System.IO;
using userinterface.Models.Script.Backend;

namespace userinterface.Models.Script
{
    public class Transpiler
    {
        public const int MaxScriptFileLength = 0xFFFF;
        public const string ScriptPath = "";

        public Transpiler()
        {
            StreamReader file;

            try
            {
                file = File.OpenText(ScriptPath);
            }
            catch
            {
                // something wrong
                return;
            }
            

            if (file.BaseStream.Length > MaxScriptFileLength)
            {
                // Tell the user to stop trolling
            }

            Tokenizer tokenizer = new(file.ReadToEnd());
        }
    }
}
