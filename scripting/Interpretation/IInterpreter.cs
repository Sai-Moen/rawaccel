using scripting.Common;
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
public sealed class InterpreterException : ScriptException
{
    public InterpreterException(string message) : base(message) { }
}

/// <summary>
/// Exception for errors relating to generating a program.
/// </summary>
public sealed class ProgramException : GenerationException
{
    public ProgramException(string message) : base(message) { }

    public ProgramException(string message, uint line) : base(message, line) { }
}