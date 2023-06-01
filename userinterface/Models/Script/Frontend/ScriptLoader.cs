using System;
using System.IO;

namespace userinterface.Models.Script.Frontend
{
    public static class ScriptLoader
    {
        public const int MaxScriptFileLength = 0xFFFF;

        public static string LoadScript(string scriptPath)
        {
            if (!File.Exists(scriptPath))
            {
                throw new FileNotFoundException();
            }
            else if (new FileInfo(scriptPath).Length > MaxScriptFileLength)
            {
                throw new NotImplementedException("Give an actual error here");
            }

            return File.ReadAllText(scriptPath);
        }
    }
}
