using System;
using userinterface.Models.Script.Generation;
using userinterface.Models.Script.Interaction;

namespace userinterface.Models.Script
{
    /// <summary>
    /// Combines all components of Script.
    /// </summary>
    public class Script
    {
        private Interpreter? _interpreter;

        public Script(ScriptInterfaceType type)
        {
            UI = ScriptInterface.Factory(type);
        }

        public IScriptInterface UI { get; }

        /// <summary>
        /// Returns Interpreter instance if one was loaded,
        /// otherwise throws <see cref="LoaderException"/>.
        /// </summary>
        public Interpreter Interpreter
        {
            get
            {
                return _interpreter ?? throw new LoaderException("No script loaded!");
            }

            private set
            {
                _interpreter = value;
            }
        }

        /// <summary>
        /// Attempts to load a RawAccelScript script from <paramref name="scriptPath"/>.
        /// Throws <see cref="ScriptException"/> on bad script input.
        /// </summary>
        public void LoadScript(string scriptPath)
        {
            string script = ScriptLoader.LoadScript(scriptPath);
            Tokenizer tokenizer = new(script);
            Parser parser = new(tokenizer.TokenList);
            Interpreter = new(parser.Parameters, parser.Variables, parser.TokenCode);
        }

        public Parameters GetDefaults()
        {
            return Interpreter.Defaults;
        }

        public Parameters GetParameters()
        {
            return Interpreter.Settings;
        }

        public void SetParameters(Parameters parameters)
        {
            Interpreter.Settings = parameters;
        }
    }

    /// <summary>
    /// Exception to derive from when doing anything inside the Script namespace.
    /// </summary>
    public abstract class ScriptException : Exception
    {
        public ScriptException(string message) : base(message) { }
    }
}
