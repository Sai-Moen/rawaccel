using System;
using userinterface.Models.Script.Generation;
using userinterface.Models.Script.Interaction;

namespace userinterface.Models.Script
{
    public class Transpiler
    {
        public const string ScriptPath = @"Scripts/"; // Maybe move to constants and remove debugpath later
        public const string __DebugPath = @"../../../Models/Script/Spec/example.rascript";

        private readonly IScriptInterface UI;

        public Transpiler(string scriptPath)
        {
#if DEBUG
            UI = ScriptInterface.Factory(ScriptInterfaceType.Debug);
#else
            UI = ScriptInterface.Factory(ScriptInterfaceType.Release);
#endif
            Transpile(scriptPath);
        }

        private void Transpile(string scriptPath)
        {
            try
            {
                string script = ScriptLoader.LoadScript(scriptPath);
                Tokenizer tokenizer = new(script);
                Parser parser = new(tokenizer.TokenList);
#if DEBUG
                foreach (Token token in tokenizer.TokenList)
                {
                    UI.HandleMessage($"{token.Line}:".PadRight(4) + $" {token.Type} ".PadRight(16) + token.Symbol);
                }
#else
#endif
            }
            catch (TranspilerException e)
            {
                UI.HandleException(e);
                return;
            }
        }
    }

    public class TranspilerException : Exception
    {
        public TranspilerException(string message)
            : base(message)
        {
        }
    }
}
