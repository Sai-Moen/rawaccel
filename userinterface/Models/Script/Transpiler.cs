using System;
using userinterface.Models.Script.Backend;
using userinterface.Models.Script.Frontend;

namespace userinterface.Models.Script
{
    public class Transpiler
    {
        public const string ScriptPath = @"Scripts/"; // Maybe move to constants and remove debugpath later
        public const string __DebugPath = @"../../../Models/Script/Spec/example.rascript";

        private readonly IScriptUI UI;

        public Transpiler(string scriptPath)
        {
#if DEBUG
            UI = ScriptUIFactory.GetScriptUI(ScriptUI.CommandLine);
#else
            UI = ScriptUIFactory.GetScriptUI(ScriptUI.Graphical);
#endif
            Transpile(scriptPath);
        }

        private void Transpile(string scriptPath)
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
                UI.HandleMessage($"{token.Kind} ->".PadRight(16) + token.Word);
            }
        }
    }
}
