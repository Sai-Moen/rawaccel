using userspace_backend.ScriptingLanguage.Compiler.CodeGen;
using userspace_backend.ScriptingLanguage.Script;

namespace userspace_backend.ScriptingLanguage.Interpreter;

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
    /// The methods that it exposes will automatically call Init and Stabilize when needed.
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
    void Stabilize();

    /// <summary>
    /// Executes a program.
    /// </summary>
    /// <param name="program">The program.</param>
    void ExecuteProgram(Program program);

    /// <summary>
    /// Executes a program with a stack given by the user.
    /// </summary>
    /// <param name="program">The program.</param>
    /// <param name="stack">The given stack.</param>
    void ExecuteProgram(Program program, ProgramStack stack);
}

/// <summary>
/// Exception for interpretation-related errors.
/// </summary>
public sealed class InterpreterException(string message)
    : ScriptException(message)
{ }
