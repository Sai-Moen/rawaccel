using System.Collections.Generic;
using System.Diagnostics;
using userspace_backend.ScriptingLanguage.Generating;
using userspace_backend.ScriptingLanguage.Interpreting;
using userspace_backend.ScriptingLanguage.Lexing;
using userspace_backend.ScriptingLanguage.Parsing;
using userspace_backend.ScriptingLanguage.Script.CallbackImpl;

namespace userspace_backend.ScriptingLanguage.Script.CallbackImpl
{
    public class Distribution
    {
        internal const string NAME = "distribution";

        private readonly int amount;
        private readonly Program program;

        internal Distribution(ParsedCallback parsed, Emitter emitter)
        {
            Debug.Assert(parsed.Name == NAME);

            IList<Token> args = parsed.Args;
            switch (args.Count)
            {
                case 0:
                    amount = Constants.LUT_POINTS_CAPACITY;
                    break;
                case 1:
                    {
                        Token first = args[0];
                        amount = (int)Number.Parse(emitter.GetSymbol(first), first);
                    }

                    if (amount <= 0 || amount > Constants.LUT_POINTS_CAPACITY)
                    {
                        throw new GenerationException($"Amount argument out of range! range: [1, {Constants.LUT_POINTS_CAPACITY}]");
                    }
                    break;
                default:
                    throw new GenerationException(
                        $"Distribution Callback only has an optional 'amount' argument, but {args.Count} were given.");
            }

            program = emitter.Emit(parsed.Code);
        }

        public double[] Distribute(IInterpreter interpreter)
        {
            interpreter.Init();
            interpreter.X = 0;

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