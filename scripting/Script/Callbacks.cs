using scripting.Common;
using scripting.Interpretation;
using scripting.Script.CallbackImpl;
using scripting.Semantical;
using scripting.Syntactical;
using System.Collections.Generic;

namespace scripting.Script;

public class Calculation
{
    internal const string NAME = "";

    private readonly Program program;

    internal Calculation(Program program)
    {
        this.program = program;
    }

    public double[] Calculate(IInterpreter interpreter, double[] xs)
    {
        interpreter.Init();

        int len = xs.Length;
        double[] ys = new double[len];
        for (int i = 0; i < len; i++)
        {
            interpreter.X = xs[i];
            interpreter.ExecuteProgram(program);
            ys[i] = interpreter.Y;

            interpreter.Stabilize(true);
        }
        return ys;
    }
}

public partial class Callbacks
{
    private readonly Dictionary<string, object> callbacks = [];

    internal Callbacks(ParsedCallback calculation, IMemoryMap addresses)
    {
        Calculation = new(new(calculation.Code, addresses));
    }

    public Calculation Calculation { get; }

    public double Calculate(IInterpreter interpreter, double x)
    {
        return Calculation.Calculate(interpreter, [x])[0];
    }

    public double[] Calculate(IInterpreter interpreter, double[] xs)
    {
        return Calculation.Calculate(interpreter, xs);
    }

    internal void Add(ParsedCallback parsed, IMemoryMap addresses)
    {
        string name = parsed.Name;
        if (name == Calculation.NAME)
            return;

        callbacks.Add(name, CallbackFactory.CreateCallback(parsed, addresses));
    }

    private object? Get(string key)
    {
        return callbacks.TryGetValue(key, out var value) ? value : null;
    }
}

internal static class CallbackFactory
{
    internal static object CreateCallback(ParsedCallback parsed, IMemoryMap addresses) => parsed.Name switch
    {
        Distribution.NAME => new Distribution(parsed, addresses),

        _ => throw new GenerationException("Unknown Callback was attempted to be implemented!")
    };
}