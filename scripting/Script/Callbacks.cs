using scripting.Common;
using scripting.Interpreting;
using scripting.Script.CallbackImpl;
using scripting.Generating;
using scripting.Parsing;
using System.Collections.Generic;

namespace scripting.Script;

public partial class Callbacks
{
    private readonly IInterpreter interpreter;
    private readonly Dictionary<string, object> callbacks = [];

    internal Callbacks(IInterpreter interpreter, ParsedCallback calculation, IMemoryMap addresses)
    {
        this.interpreter = interpreter;
        Emitter emitter = new(addresses);
        Calculation = new(emitter.Emit(calculation.Code));
    }

    internal Calculation Calculation { get; }

    public double Calculate(double x)
    {
        return Calculation.Calculate(interpreter, [x])[0];
    }

    public double[] Calculate(double[] xs)
    {
        return Calculation.Calculate(interpreter, xs);
    }

    internal void Add(ParsedCallback parsed, IMemoryMap addresses)
    {
        string name = parsed.Name;
        if (name != Calculation.NAME)
        {
            callbacks.Add(name, CreateCallback(parsed, addresses));
        }
    }

    internal static object CreateCallback(ParsedCallback parsed, IMemoryMap addresses) => parsed.Name switch
    {
        Distribution.NAME => new Distribution(parsed, addresses),

        _ => throw new GenerationException("Unknown Callback was attempted to be implemented!")
    };

    private object? Get(string key)
    {
        return callbacks.TryGetValue(key, out var value) ? value : null;
    }
}

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

            interpreter.Stabilize();
        }
        return ys;
    }
}
