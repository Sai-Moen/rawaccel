using System;
using System.Collections.Generic;
using userspace_backend.ScriptingLanguage.Compiler.Parser;
using userspace_backend.ScriptingLanguage.Compiler.Tokenizer;
using userspace_backend.ScriptingLanguage.Script;

namespace userspace_backend.ScriptingLanguage.Compiler.CodeGen;

/// <summary>
/// Defines the API of a RawAccelScript Emitter.
/// </summary>
public interface IEmitter
{
    /// <summary>
    /// Runs the emitter, produces the program from an expression.
    /// </summary>
    /// <param name="code">Code as expression.</param>
    /// <returns>A Program instance.</returns>
    /// <exception cref="EmitException"/>
    Program Emit(IList<Token> code);

    /// <summary>
    /// Runs the emitter, produces the program from ASTs.
    /// </summary>
    /// <param name="code">Code as ASTs.</param>
    /// <returns>A Program instance.</returns>
    /// <exception cref="EmitException"/>
    Program Emit(IList<ASTNode> code);

    /// <summary>
    /// Runs the emitter, produces the program for a user-defined function.
    /// </summary>
    /// <param name="args">The argument(s) list.</param>
    /// <param name="code">The function body code.</param>
    /// <returns>A Program instance.</returns>
    /// <exception cref="EmitException"/>
    Program EmitFunction(IList<Token> args, IList<ASTNode> code);
}

/// <summary>
/// Represents a program consisting of executable Instructions.
/// </summary>
public record Program(byte[] ByteCode, StaticData Data)
{
    public int Length => ByteCode.Length;
    public int Arity { get; set; } = 0;

    public byte this[CodeAddress index] => ByteCode[index];
    public Number this[DataAddress index] => Data[index];

    public ReadOnlySpan<byte> ExtractAddress(ref CodeAddress c)
    {
        int addressLength = ((InstructionType)this[c]).AddressLength();
        ReadOnlySpan<byte> address = new(ByteCode, c.Address + 1, addressLength);
        c += addressLength;
        return address;
    }
}

/// <summary>
/// Exception for errors related to emitting bytecode into a program.
/// </summary>
public sealed class EmitException : CompilationException
{
    public EmitException(string message)
        : base(message)
    { }

    public EmitException(string message, Token suspect)
        : base(message, suspect)
    { }
}