using System.Diagnostics;
using userspace_backend.ScriptingLanguage.Compiler;

namespace userspace_backend.ScriptingLanguage;

public class ScriptFile : IScriptFile
{
    private readonly Interpreter interpreter;
    private readonly Calculation? calculation;
    private readonly Distribution? distribution;

    public ScriptFile(Context context, AST ast)
    {
        Description = ast.Description;

        Emitter emitter = new(context);
        interpreter = new(ast, emitter); // heavy side-effects on emitter btw

        foreach (ASTNode node in ast.Declarations)
        {
            if (node.Tag != ASTTag.Callback)
                continue;

            ASTCallback callback = node.Union.astCallback;
            Token identifier = callback.Identifier;
            Debug.Assert(identifier.Kind == TokenKind.CallbackName);
            ExtraIndexCallback index = (ExtraIndexCallback)identifier.ExtraIndex;
            switch (index)
            {
                case ExtraIndexCallback.Calculation:
                    if (calculation is not null)
                        throw new ScriptException("Duplicate calculation callback!");

                    calculation = new Calculation(callback, emitter);
                    break;
                case ExtraIndexCallback.Distribution:
                    if (distribution is not null)
                        throw new ScriptException("Duplicate distribution callback!");

                    distribution = new Distribution(callback, emitter);
                    break;
                default:
                    throw new ScriptException($"Corrupted ExtraIndexCallback: '{index}'!");
            }
        }

        if (calculation is null)
            throw new ScriptException("Implementing the calculation callback is mandatory!");
    }

    public string Description { get; }

    public ReadOnlyParameters Defaults => interpreter.Defaults;
    public Parameters Settings => interpreter.Settings;

    public double[] RunScript(double desiredMaxSpeed)
    {
        double[] xs;
        if (distribution is not null)
            xs = distribution.Distribute(interpreter);
        else
            xs = DefaultDistribution(desiredMaxSpeed);
        return Calculate(xs);
    }

    public double[] Calculate(double[] xs)
    {
        return calculation!.Calculate(interpreter, xs);
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

internal class Calculation
{
    private readonly Program program;

    internal Calculation(ASTCallback ast, Emitter emitter)
    {
        Debug.Assert(ast.Expressions.Length == 0);
        program = emitter.Emit(ast.Code);
    }

    internal double[] Calculate(Interpreter interpreter, double[] xs)
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

internal class Distribution
{
    private readonly Program argsProgram;
    private readonly Program program;

    internal Distribution(ASTCallback ast, Emitter emitter)
    {
        Token[] args = ast.Expressions;
        if (args.Length == 0)
        {
            // bit hacky, but it will work for now
            args = [Tokens.GetReserved(Tokens.CONST_CAPACITY)];
        }
        argsProgram = emitter.Emit(args);
        argsProgram.Arity = 1;

        program = emitter.Emit(ast.Code);
    }

    internal double[] Distribute(Interpreter interpreter)
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
