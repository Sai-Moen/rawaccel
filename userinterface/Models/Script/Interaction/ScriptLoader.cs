using System.IO;

namespace userinterface.Models.Script.Interaction
{
    public static class ScriptLoader
    {
        public const long MaxScriptFileLength = 0xFFFF;

        public static string LoadScript(string scriptPath)
        {
            if (!File.Exists(scriptPath))
            {
                throw new TranspilerException("File not found!");
            }
            else if (new FileInfo(scriptPath).Length > MaxScriptFileLength)
            {
                throw new TranspilerException("File too big!");
            }

            try
            {
                return File.ReadAllText(scriptPath);
            }
            catch
            {
                // Could differentiate between certain exceptions,
                // keep in mind that File.Exists already catches a decent amount.
                throw new TranspilerException("File not readable!");
            }
        }
    }
}
