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

#if DEBUG
            Test(Interpreter);
#endif
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

        private void Test(Interpreter interpreter)
        {
            const int cap = 0x1000;
            double[] ys = new double[cap];

            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < cap; i++)
            {
                ys[i] = interpreter.Calculate(i);
            }
            sw.Stop();

            UI.HandleMessage(sw.Elapsed.TotalMilliseconds.ToString());
            UI.HandleMessage((ys[16] * 16).ToString());
        }
    }

    public class ScriptException : Exception
    {
        public ScriptException(string message) : base(message) { }
    }
}
