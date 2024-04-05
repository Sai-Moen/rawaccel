using scripting.Common;

namespace scripting.Semantical;

/// <summary>
/// Exception for errors relating to emitting bytecode into a program.
/// </summary>
public sealed class EmitException : GenerationException
{
    public EmitException(string message) : base(message) { }

    public EmitException(string message, uint line) : base(message, line) { }
}