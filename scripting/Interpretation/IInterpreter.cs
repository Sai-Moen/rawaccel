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
    Parameters Defaults { get; }

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
    /// Performs a calculation on the currently loaded script and settings.
    /// </summary>
    /// <param name="x">The input value to inject into the loaded script and settings.</param>
    /// <returns>The resulting output value.</returns>
    double Calculate(double x);
}

/// <summary>
/// Exception for interpretation-related errors.
/// </summary>
public sealed class InterpreterException : GenerationException
{
    public InterpreterException(string message) : base(message) { }

    public InterpreterException(string message, uint line) : base(message, line) { }
}
