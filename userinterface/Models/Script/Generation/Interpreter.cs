using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;

namespace userinterface.Models.Script.Generation
{
    public class Interpreter
    {
        private readonly MemoryMap Addresses = new();

        private readonly MemoryHeap Stable = new();

        private readonly MemoryHeap Volatile = new();

        private readonly Program MainProgram;

        private readonly Program[] Startup;

        public Interpreter(
            List<ParameterAssignment> parameters,
            List<VariableAssignment> variables,
            TokenList code)
        {
            Debug.Assert(parameters.Count <= Tokens.MAX_PARAMETERS);
            for (int i = 0; i < parameters.Count; i++)
            {
                ParameterAssignment parameter = parameters[i];

                string name = parameter.Token.Base.Symbol;

                Addresses.Add(name, new(i));

                double value = parameter.Value ??
                    throw new InterpreterException(parameter.Token.Line, "Parameter Value not set!");

                Defaults[i] = new(name, value);
            }

            Startup = new Program[variables.Count];

            Debug.Assert(variables.Count <= Tokens.MAX_VARIABLES);
            for (int i = 0; i < variables.Count; i++)
            {
                int j = i + Tokens.MAX_PARAMETERS;

                VariableAssignment variable = variables[i];

                string name = variable.Token.Base.Symbol;

                Addresses.Add(name, new(j));

                Expression expr = variable.Expr ??
                    throw new InterpreterException(variable.Token.Line, "Variable Expr not set!");

                Startup[i] = new(expr, Addresses);
            }

            MainProgram = new(new(code), Addresses);
        }

        public ParameterNameValuePairs Defaults { get; } = new();

        public ParameterNameValuePairs CurrentSettings { get; set; } = new();

        public void Init()
        {

        }

        private void Restore()
        {
            Volatile.RestoreTo(Stable);
        }

        private static void InterpreterError(string error)
        {
            throw new InterpreterException(error);
        }
    }

    public class InterpreterException : ScriptException
    {
        public InterpreterException(string message) : base(message) { }

        public InterpreterException(uint line, string message) : base($"Line {line}: {message}") { }
    }
}
