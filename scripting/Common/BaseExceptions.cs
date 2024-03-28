using System;

namespace scripting.Common;

/// <summary>
/// Exception to derive from when doing anything inside the scripting namespace.
/// </summary>
public class ScriptException(string message) : Exception(message)
{
}

/// <summary>
/// Base Exception used for errors in all stages of code generation.
/// </summary>
public class GenerationException : ScriptException
{
    public GenerationException(string message) : base(message) { }

    public GenerationException(string message, uint line) : base($"Line {line}: {message}") { }
}
