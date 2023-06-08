using System;
using System.Text;
using userinterface.Models.Script.Generation;
using userinterface.Models.Script.Interaction;

namespace userinterface.Models.Script
{
    public class Script
    {
        public const string ScriptPath = @"Scripts/"; // Maybe move to constants and remove debugpath later
        public const string __DebugPath = @"../../../Models/Script/Spec/test.rascript";

        private readonly IScriptInterface UI;

        private Interpreter _interpreter;

        public Script()
        {
#if DEBUG
            UI = ScriptInterface.Factory(ScriptInterfaceType.Debug);
#else
            UI = ScriptInterface.Factory(ScriptInterfaceType.Release);
#endif
        }

        public void LoadScript(string scriptPath)
        {
            try
            {
                Tokenizer tokenizer = new(ScriptLoader.LoadScript(scriptPath));
                Parser parser = new(tokenizer.TokenList);
                _interpreter = new();
#if DEBUG
                StringBuilder builder = new();
                foreach (Token token in tokenizer.TokenList)
                {
                    builder.AppendLine(
                        $"{token.Line}:".PadRight(4) + $" {token.Base.Type} ".PadRight(32) + token.Base.Symbol);
                }

                builder.AppendLine().AppendLine("Variables:");
                foreach (VariableAssignment a in parser.Variables)
                {
                    if (a.IsExpression)
                    {
                        builder.AppendLine(
                            $"{a.Token.Base.Symbol}: ");
                        foreach (Token token in a.Expr!.Tokens)
                        {
                            builder.AppendLine('\t' + token.ToString());
                        }
                    }
                    else
                    {
                        builder.AppendLine(
                            $"{a.Token.Base.Symbol}: ".PadRight(4) + a.Value);
                    }
                }

                builder.AppendLine().AppendLine("Interpreter code:");
                foreach (Token token in parser.TokenCode)
                {
                    builder.AppendLine(
                        $"{token.Line}:".PadRight(4) + $" {token.Base.Type} ".PadRight(32) + token.Base.Symbol);
                }

                UI.HandleMessage(builder.ToString());
#endif
            }
            catch (ScriptException e)
            {
                UI.HandleException(e);
            }
        }
    }

    public class ScriptException : Exception
    {
        public ScriptException(string message)
            : base(message)
        {
        }
    }
}
