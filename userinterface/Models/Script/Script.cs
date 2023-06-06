using System;
using System.Text;
using userinterface.Models.Script.Generation;
using userinterface.Models.Script.Interaction;

namespace userinterface.Models.Script
{
    public class Script
    {
        public const string ScriptPath = @"Scripts/"; // Maybe move to constants and remove debugpath later
        public const string __DebugPath = @"../../../Models/Script/Spec/example.rascript";

        private readonly IScriptInterface UI;

        public Script(string scriptPath)
        {
#if DEBUG
            UI = ScriptInterface.Factory(ScriptInterfaceType.Debug);
#else
            UI = ScriptInterface.Factory(ScriptInterfaceType.Release);
#endif
            Run(scriptPath);
        }

        private void Run(string scriptPath)
        {
            try
            {
                string script = ScriptLoader.LoadScript(scriptPath);
                Tokenizer tokenizer = new(script);
                Parser parser = new(tokenizer.TokenList);
#if DEBUG
                StringBuilder builder = new();
                foreach (Token token in tokenizer.TokenList)
                {
                    builder.AppendLine(
                        $"{token.Line}:".PadRight(4) + $" {token.Base.Type} ".PadRight(32) + token.Base.Symbol);
                }
                UI.HandleMessage(builder.ToString());
#else
#endif
            }
            catch (TranspilerException e)
            {
                UI.HandleException(e);
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
