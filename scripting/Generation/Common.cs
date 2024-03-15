using System.Diagnostics;

namespace scripting.Generation;

/// <summary>
/// Constants used for all of Script.
/// </summary>
public static class Constants
{
    public const int MAX_IDENTIFIER_LEN = 0x10;
    public const int MAX_NUMBER_LEN = 0x20;

    public const int CAPACITY = MemoryAddress.CAPACITY;
    public const int MAX_PARAMETERS = 8;
    public const int MAX_VARIABLES = CAPACITY - MAX_PARAMETERS;

    static Constants()
    {
        Debug.Assert(CAPACITY == MAX_PARAMETERS + MAX_VARIABLES);
    }
}

/// <summary>
/// Exception used for errors in all stages of code generation.
/// </summary>
public abstract class GenerationException : ScriptException
{
    public GenerationException(string message) : base(message) { }

    public GenerationException(string message, uint line) : base($"Line {line}: {message}") { }
}
