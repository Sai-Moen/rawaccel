using System.Diagnostics;
using userspace_backend.ScriptingLanguage.Compiler.CodeGen;
using userspace_backend.ScriptingLanguage.Compiler.Parser;
using userspace_backend.ScriptingLanguage.Compiler.Tokenizer;
using userspace_backend.ScriptingLanguage.Interpreter;
using userspace_backend.ScriptingLanguage.Script.CallbackImpl;

namespace userspace_backend.ScriptingLanguage.Script.CallbackImpl
{
    public class Distribution
    {
        internal const string NAME = "distribution";

        private readonly Program argsProgram;
        private readonly Program program;

        internal Distribution(ParsedCallback parsed, EmitterImpl emitter)
        {
            Debug.Assert(parsed.Name == NAME);

            Token[] args = parsed.Args;
            if (args.Length == 0)
            {
                // bit hacky, but it will work for now
                args = [Tokens.GetReserved(Tokens.CONST_CAPACITY)];
            }
            argsProgram = emitter.Emit(args);
            argsProgram.Arity = 1;

            program = emitter.Emit(parsed.Code);
        }

        public double[] Distribute(IInterpreter interpreter)
        {
            interpreter.Init();
            interpreter.X = 0;

            ProgramStack stack = [];
            interpreter.ExecuteProgram(argsProgram, stack);
            Debug.Assert(stack.Count == 1, "Bug in InterpreterImpl that allows for 'Count != Arity'?");

            int amount = (int)stack[0];
            if (amount < 1 || amount > Constants.LUT_POINTS_CAPACITY)
                throw new InterpreterException(
                    $"Amount argument out of range! range: [1, {Constants.LUT_POINTS_CAPACITY}]");

            double[] inputs = new double[amount];
            for (int i = 0; i < amount; i++)
            {
                // X is stateful in this callback
                interpreter.ExecuteProgram(program);
                inputs[i] = interpreter.X;

                interpreter.Stabilize();
            }
            return inputs;
        }
    }
}

namespace userspace_backend.ScriptingLanguage.Script
{
    public partial class Callbacks
    {
        public bool HasDistribution => Distribution is not null;

        internal Distribution? Distribution => Get(Distribution.NAME) as Distribution;

        public double[] Distribute()
        {
            Distribution? distribution = Distribution;
            if (distribution is null)
            {
                // could maybe throw here
                return [];
            }

            return distribution.Distribute(interpreter);
        }
    }
}