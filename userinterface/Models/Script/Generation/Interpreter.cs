using System.Collections.Generic;
using System.Diagnostics;

namespace userinterface.Models.Script.Generation
{
    public class Interpreter
    {
        private readonly Assignment Assignment;

        private readonly HeapMemory Volatile = new();

        private readonly HeapMemory Stable = new();

        private readonly Instruction[] Code;

        private readonly Dictionary<string, MemoryAddress> Addresses = new();

        public double X { get; set; }

        public double Y { get; }

        public Interpreter(
            List<ParameterAssignment> parameters,
            List<VariableAssignment> variables,
            TokenList code)
        {
            Assignment = new(parameters, variables);

            Debug.Assert(parameters.Count <= Tokens.MAX_PARAMETERS);
            for (int i = 0; i < parameters.Count; i++)
            {
                ParameterAssignment parameter = parameters[i];

                Stable[i] = parameter.Value ?? 0;

                Addresses.Add(parameter.Token.Base.Symbol, new(i));
            }

            Debug.Assert(variables.Count <= Tokens.MAX_VARIABLES);
            for (int i = 0; i < variables.Count; i++)
            {
                VariableAssignment variable = variables[i];

                int j = i + Tokens.MAX_PARAMETERS;

                Addresses.Add(variable.Token.Base.Symbol, new(j));
            }

            Code = Instructions.Emit(new(code), Addresses);

            Restore();
        }

        private void Restore()
        {
            Volatile.Restore(Stable);
        }

        private void InterpreterError(string error)
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
