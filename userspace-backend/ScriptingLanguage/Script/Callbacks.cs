﻿using System.Collections.Generic;
using userspace_backend.ScriptingLanguage.Compiler.CodeGen;
using userspace_backend.ScriptingLanguage.Compiler.Parser;
using userspace_backend.ScriptingLanguage.Interpreter;
using userspace_backend.ScriptingLanguage.Script.CallbackImpl;

namespace userspace_backend.ScriptingLanguage.Script;

public partial class Callbacks
{
    private readonly IInterpreter interpreter;
    private readonly Dictionary<string, object> callbacks = [];

    internal Callbacks(InterpreterImpl interpreter, ParsedCallback calculation, EmitterImpl emitter)
    {
        this.interpreter = interpreter;
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

    internal void Add(ParsedCallback parsed, EmitterImpl emitter)
    {
        string name = parsed.Name;
        if (name != Calculation.NAME)
            callbacks.Add(name, CreateCallback(parsed, emitter));
    }

    internal static object CreateCallback(ParsedCallback parsed, EmitterImpl emitter) => parsed.Name switch
    {
        Distribution.NAME => new Distribution(parsed, emitter),

        _ => throw new CompilationException("Unknown Callback was attempted to be implemented!")
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

            interpreter.Y = Number.DEFAULT_Y;
            interpreter.Stabilize();
        }
        return ys;
    }
}
