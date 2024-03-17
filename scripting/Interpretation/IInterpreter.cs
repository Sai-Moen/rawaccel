using scripting.Common;
using scripting.Syntactical;

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
    /// The current values of all parameters.
    /// Setting this property will automatically update the values of all variables.
    /// </summary>
    Parameters Settings { get; set; }

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
