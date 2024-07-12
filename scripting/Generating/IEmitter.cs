using scripting.Common;
using scripting.Script;
using scripting.Parsing;
using System.Collections.Generic;

namespace scripting.Generating;

/// <summary>
/// Defines the API of a RawAccelScript Emitter.
/// </summary>
public interface IEmitter
{
    /// <summary>
    /// Runs the emitter, produces the program from an expression.
    /// </summary>
    /// <param name="code">Code as expression</param>
    /// <returns>A Program instance</returns>
    Program Emit(ITokenList code);

    /// <summary>
    /// Runs the emitter, produces the program from ASTs.
    /// </summary>
    /// <param name="code">Code as ASTs</param>
    /// <returns>A Program instance</returns>
    /// <exception cref="EmitException"/>
    Program Emit(IList<ASTNode> code);
}

/// <summary>
/// Represents a program consisting of executable Instructions.
/// </summary>
public record Program(InstructionUnion[] Instructions, StaticData Data)
{
    public int Length => Instructions.Length;

    public InstructionUnion this[CodeAddress index] => Instructions[index];
    public Number this[DataAddress index] => Data[index];

    public InstructionOperand GetOperandFromNext(ref CodeAddress c) => this[++c].operand;
}

/// <summary>
/// Exception for errors relating to emitting bytecode into a program.
/// </summary>
public sealed class EmitException : GenerationException
{
    public EmitException(string message) : base(message) { }

    public EmitException(string message, uint line) : base(message, line) { }
}