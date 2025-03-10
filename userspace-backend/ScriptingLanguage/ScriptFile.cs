using System;
using System.Diagnostics;
using userspace_backend.ScriptingLanguage.Compiler;

namespace userspace_backend.ScriptingLanguage;

public enum CallbackKind : byte
{
    Calculation,
    Distribution,

    Count
}

[Flags]
public enum CallbackFlags : byte
{
    None,

    Calculation = 1 << CallbackKind.Calculation,
    Distribution = 1 << CallbackKind.Distribution,
}

public class ScriptFile : IScriptFile
{
    private readonly Interpreter interpreter;
    private readonly object[] callbacks = new object[(int)CallbackKind.Count];
    private readonly CallbackFlags flags = CallbackFlags.Calculation;

    public ScriptFile(Context context, AST ast)
    {
        Description = ast.Description;

        Emitter emitter = new(context);
        interpreter = new(ast, emitter); // heavy side-effects on emitter btw

        // temporary solution
        callbacks[(int)CallbackKind.Calculation] = new Calculation(ast.Callbacks[0], emitter);
        if (ast.Callbacks.Count == 2)
        {
            callbacks[(int)CallbackKind.Distribution] = new Distribution(ast.Callbacks[1], emitter);
            flags |= CallbackFlags.Distribution;
        }
        else
        {
            Debug.Assert(ast.Callbacks.Count == 1, "Expected only calculation in this temporary solution...");
        }
    }

    public string Description { get; }

    public ReadOnlyParameters Defaults => interpreter.Defaults;
    public Parameters Settings => interpreter.Settings;

    private Calculation Calculation => (Calculation)callbacks[(int)CallbackKind.Calculation];
    private Distribution Distribution => (Distribution)callbacks[(int)CallbackKind.Distribution];

    public double[] RunScript(double desiredMaxSpeed)
    {
        double[] xs;
        if ((flags & CallbackFlags.Distribution) == CallbackFlags.Distribution)
            xs = Distribution.Distribute(interpreter);
        else
            xs = DefaultDistribution(desiredMaxSpeed);
        return Calculate(xs);
    }

    public double[] Calculate(double[] xs)
    {
        return Calculation.Calculate(interpreter, xs);
    }

    private static double[] DefaultDistribution(double desiredSpeed)
    {
        const int cap = Constants.LUT_POINTS_CAPACITY;

        // geometric progression or floating point magic might be preferred over this arithmetic progression
        double step = desiredSpeed / cap;

        double[] xs = new double[cap];
        for (uint i = 0; i < cap; i++)
            xs[i] = i * step;
        return xs;
    }
}

public class Calculation
{
    internal const string NAME = "";

    private readonly Program program;

    internal Calculation(ParsedCallback parsed, Emitter emitter)
    {
        Debug.Assert(parsed.Name == NAME);
        Debug.Assert(parsed.Args.Length == 0);
        program = emitter.Emit(parsed.Code);
    }

    public double[] Calculate(Interpreter interpreter, double[] xs)
    {
        interpreter.Init();

        int len = xs.Length;
        double[] ys = new double[len];
        for (int i = 0; i < len; i++)
        {
            interpreter.X = xs[i];
            interpreter.ExecuteProgram(program);
            ys[i] = interpreter.Y;

            interpreter.Y = Number.DEFAULT_Y;
            interpreter.Stabilize();
        }
        return ys;
    }
}

public class Distribution
{
    internal const string NAME = "distribution";

    private readonly Program argsProgram;
    private readonly Program program;

    internal Distribution(ParsedCallback parsed, Emitter emitter)
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

    public double[] Distribute(Interpreter interpreter)
    {
        interpreter.Init();
        interpreter.X = 0;

        ProgramStack stack = [];
        interpreter.ExecuteProgram(argsProgram, stack);
        Debug.Assert(stack.Count == 1, "Bug in Interpreter that allows for 'Count != Arity'?");

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
