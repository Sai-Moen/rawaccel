﻿using scripting.Common;
using scripting.Script;

namespace scripting.Interpretation;

/// <summary>
/// Defines the API of a RawAccelScript interpreter.
/// </summary>
public interface IInterpreter
{
    /// <summary>
    /// The description of the script.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// The parameters and their default values, according to the script.
    /// </summary>
    ReadOnlyParameters Defaults { get; }

    /// <summary>
    /// The current state of all parameters.
    /// </summary>
    Parameters Settings { get; }

    /// <summary>
    /// An object to access any options defined by the script.
    /// </summary>
    Options Options { get; }

    /// <summary>
    /// Sets internal memory so that it accurately reflects the current settings.
    /// Call this before performing a bunch of calculations.
    /// </summary>
    void Init();

    /// <summary>
    /// Runs a calculation with the current state.
    /// </summary>
    /// <param name="x">the input value to inject</param>
    /// <returns>The resulting output value.</returns>
    double Calculate(double x);
}

/// <summary>
/// Exception for interpretation-related errors.
/// </summary>
public sealed class InterpreterException(string message) : ScriptException(message)
{
}

/// <summary>
/// Exception for errors relating to emitting bytecode into a program.
/// </summary>
public sealed class EmitException : GenerationException
{
    public EmitException(string message) : base(message) { }

    public EmitException(string message, uint line) : base(message, line) { }
}