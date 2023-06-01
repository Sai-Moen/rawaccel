using System;
using userinterface.Models.Script.Backend;
using userinterface.Models.Script.Frontend;

namespace userinterface.Models.Script
{
    public static class Transpiler
    {
        public const string ScriptPath = @"Scripts\"; // Maybe move to constants and remove debugpath later
        public const string __DebugPath = @"C:\Users\SaiMoen\dev\src\rawaccel_SaiMoen\userinterface\Models\Script\Spec\arc_example.rascript";

        private static readonly IScriptUI UI = ScriptUIFactory.GetScriptUI(ScriptUI.CommandLine);

        static Transpiler()
        {
#if DEBUG
#else
            UI = ScriptUIFactory.GetScriptUI(ScriptUI.Graphical);
#endif
        }

        public static void Transpile(string scriptPath)
        {
            Tokenizer tokenizer;

            try
            {
                tokenizer = new(ScriptLoader.LoadScript(scriptPath));
            }
            catch (Exception e)
            {
                UI.HandleException(e);
                return;
            }

            foreach(Token token in tokenizer.TokenList)
            {
                UI.HandleMessage($"{token.Word}: {token.Kind}");
            }
        }
    }
}
