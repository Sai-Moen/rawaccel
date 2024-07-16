using scripting.Common;
using scripting.Interpreting;
using scripting.Script.CallbackImpl;
using scripting.Generating;
using scripting.Parsing;
using System.Diagnostics;

namespace scripting.Script.CallbackImpl
{
    public class Distribution
    {
        internal const string NAME = "distribution";

        private readonly int amount;
        private readonly Program program;

        internal Distribution(ParsedCallback parsed, IMemoryMap addresses)
        {
            Debug.Assert(parsed.Name == NAME);

            ITokenList args = parsed.Args;
            switch (args.Count)
            {
                case 0:
                    amount = Constants.LUT_POINTS_CAPACITY;
                    break;
                case 1:
                    amount = (int)(Number)args[0];
                    if (amount < 0 || amount > Constants.LUT_POINTS_CAPACITY)
                    {
                        throw new GenerationException($"Amount argument out of range! range: [0, {Constants.LUT_POINTS_CAPACITY}]");
                    }
                    break;
                default:
                    throw new GenerationException(
                        $"Distribution Callback only has an optional 'amount' argument, but {args.Count} were given.");
            }

            Emitter emitter = new(addresses);
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
                inputs[i] = interpreter.Y;

                interpreter.Stabilize();
            }
            return inputs;
        }
    }
}

namespace scripting.Script
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