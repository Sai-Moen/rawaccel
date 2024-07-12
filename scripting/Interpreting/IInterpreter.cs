using scripting.Common;
using scripting.Script;
using scripting.Generating;
using System.Collections.Generic;

namespace scripting.Interpreting;

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
    /// An object to access any callbacks defined by the script.
    /// </summary>
    Callbacks Callbacks { get; }

    /// <summary>
    /// The input variable.
    /// </summary>
    Number X { get; set; }

    /// <summary>
    /// The output variable.
    /// </summary>
    Number Y { get; set; }

    /// <summary>
    /// Sets internal memory so that it accurately reflects the current settings.
    /// Call this before performing a bunch of calculations (upon callback request).
    /// </summary>
    void Init();

    /// <summary>
    /// Stabilizes after running a program s.t. it returns to the state after Init was called most recently.
    /// </summary>
    /// <param name="resetY">whether to reset Y to 1</param>
    void Stabilize(bool resetY = false);

    /// <summary>
    /// Executes a program with the interpreter's own stack. Clears stack after use.
    /// </summary>
    /// <param name="program">the program</param>
    /// <returns>the remainder of the stack as an array</returns>
    Number[] ExecuteProgram(Program program);

    /// <summary>
    /// Executes a program with the given stack. Clears stack after use.
    /// </summary>
    /// <param name="program">the program</param>
    /// <param name="stack">the stack</param>
    /// <returns>the remainder of the stack as an array</returns>
    Number[] ExecuteProgram(Program program, Stack<Number> stack);
}

/// <summary>
/// Exception for interpretation-related errors.
/// </summary>
public sealed class InterpreterException(string message) : ScriptException(message)
{
}
