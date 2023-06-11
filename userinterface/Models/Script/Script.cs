using System;
using userinterface.Models.Script.Generation;
using userinterface.Models.Script.Interaction;

namespace userinterface.Models.Script
{
    public class Script
    {
        public const string ScriptPath = @"Scripts/"; // Maybe move to constants and remove debugpath later
        public const string DebugPath = @"../../../Models/Script/Spec/arc.rascript";

        private Interpreter? _interpreter;

        public Script(ScriptInterfaceType type)
        {
            UI = ScriptInterface.Factory(type);
        }

        public IScriptInterface UI { get; }

        public Interpreter Interpreter
        {
            get
            {
                return _interpreter ?? throw new ScriptException("No script loaded!");
            }
            private set { _interpreter = value; }
        }

        /// <summary>
        /// Attempts to load a RawAccelScript script from
        /// <paramref name="scriptPath"/>.
        /// Throws <see cref="ScriptException"/> on bad script input
        /// </summary>
        public void LoadScript(string scriptPath)
        {
            string script = ScriptLoader.LoadScript(scriptPath);
            Tokenizer tokenizer = new(script);
            Parser parser = new(tokenizer.TokenList);
            Interpreter = new(parser.Parameters, parser.Variables, parser.TokenCode);
        }
    }

    public class ScriptException : Exception
    {
        public ScriptException(string message) : base(message) { }
    }
}
