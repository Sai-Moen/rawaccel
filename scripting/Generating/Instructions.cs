using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace scripting.Generating;

public enum InstructionType : byte
{
    NoOp,
    Start, End, // Helps with jumps not going out of bounds (in an otherwise correct program)
    Return, // Early return 

    // TOS = Top Of Stack
    Load, Store,       // Gets or Sets an Address in the 'heap', to/from TOS.
    LoadIn, StoreIn,   // Gets or Sets the input register (x), to/from TOS.
    LoadOut, StoreOut, // Gets or Sets the output register (y), to/from TOS.
    LoadNumber,        // Loads a number from data.
    Swap,              // Swaps the top two stack elements.

    // Branch,
    // Evaluates the TOS and jumps/skips to the next branch end marker if zero (Jz).
    // The jump itself can be unconditional (Jmp) instead, to implement loops (Jmp backwards).
    Jmp, Jz,

    // Constant,
    // Pushes a constant to the stack.
    LoadZero,
    LoadE, LoadPi, LoadTau,
    LoadCapacity,

    // Operator,
    // Does an operation on the second and first Stack item respectively,
    // Pushes the result onto the stack if the next instruction is not another operator.
    Add, Sub,
    Mul, Div, Mod,
    Pow, Exp,

    // Comparison,
    // Returns, for some condition, 1.0 when true, 0.0 when false.
    Or, And,
    Lt, Gt, Le, Ge,
    Eq, Ne, Not,

    // Function,
    // Take arguments from the stack and give a transformed version back.
    Abs, Sign, CopySign,
    Round, Trunc, Floor, Ceil, Clamp,
    Min, Max, MinM, MaxM,
    Sqrt, Cbrt,
    Log, Log2, Log10, LogB,
    Sin, Sinh, Asin, Asinh,
    Cos, Cosh, Acos, Acosh,
    Tan, Tanh, Atan, Atanh, Atan2,
    FusedMultiplyAdd, ScaleB,

    // Leave this at the bottom of the enum for obvious reasons.
    Count
}

/// <summary>
/// Provides helper methods and static checking.
/// </summary>
public static class Instructions
{
    public static byte Size(this InstructionType type) => type switch
    {
        InstructionType.Load => 2,
        InstructionType.Store => 2,
        InstructionType.LoadNumber => 2,

        InstructionType.Jz => 2,
        InstructionType.Jmp => 2,

        _ => 1,
    };

    public static bool IsInline(this InstructionType type) => type != InstructionType.Store;
}

/// <summary>
/// Flags for the second part of an Instruction.
/// </summary>
[Flags]
public enum InstructionFlags : byte
{
    None = 0,

    /// <summary>
    /// Whether this instruction expects operands.
    /// </summary>
    Continuation = 1 << 0,

    /// <summary>
    /// Whether this instruction should only be executed when the top of the stack is non-zero.
    /// </summary>
    IsConditional = 1 << 1,

    /// <summary>
    /// Whether this instruction is expecting to be replaced by a looping body.
    /// </summary>
    IsLoop = 1 << 2,
}

/// <summary>
/// Represents an Instruction that can be executed by the Interpreter.
/// </summary>
public readonly record struct Instruction(InstructionType Type, InstructionFlags Flags = InstructionFlags.None)
{
    public bool AnyFlags(InstructionFlags flags) => (Flags & flags) != InstructionFlags.None;
    public bool AllFlags(InstructionFlags flags) => (Flags & flags) == flags;
}

/// <summary>
/// Can represent any address type.
/// </summary>
public readonly struct InstructionOperand
{
    public InstructionOperand(MemoryAddress address)
    {
        Operand = address;
    }

    public InstructionOperand(DataAddress address)
    {
        Operand = address;
    }

    public InstructionOperand(CodeAddress address)
    {
        Operand = address;
    }

    public readonly ushort Operand { get; }
}

/// <summary>
/// Either an Instruction or InstructionOperand.
/// It is an untagged union, Instruction can tell if the next union will be operand or not.
/// So, if the program is laid out correctly, it can be determined what is an instruction vs operand.
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public struct InstructionUnion
{
    [FieldOffset(0)] public Instruction instruction;
    [FieldOffset(0)] public InstructionOperand operand;
}

/// <summary>
/// Represents a list of instructions.
/// </summary>
public class InstructionList : List<InstructionUnion>, IList<InstructionUnion>
{
    #region Constructors

    public InstructionList() : base() { }

    public InstructionList(int capacity) : base(capacity) { }

    #endregion

    #region Add

    public void Add(Instruction instruction)
    {
        Debug.Assert(instruction.Type.Size() == 1);

        InstructionUnion unionI = new()
        {
            instruction = instruction
        };
        Add(unionI);
    }

    public void Add(InstructionType type)
    {
        Add(new Instruction(type));
    }

    public void Add(Instruction instruction, InstructionOperand operand)
    {
        Debug.Assert(instruction.Type.Size() == 2);

        InstructionUnion unionI = new()
        {
            instruction = instruction
        };
        InstructionUnion unionO = new()
        {
            operand = operand
        };
        AddRange([unionI, unionO]);
    }

    public void Add(InstructionType type, InstructionOperand operand)
    {
        Add(new Instruction(type, InstructionFlags.Continuation), operand);
    }

    #endregion

    #region Remove

    public void RemoveAt(CodeAddress address)
    {
        base.RemoveAt(address);
    }

    #endregion

    #region Helpers

    public void SetOperand(CodeAddress index, InstructionOperand value)
    {
        this[index] = new InstructionUnion()
        {
            operand = value
        };
    }

    public void SetOperand(CodeAddress index, CodeAddress value)
    {
        SetOperand(index, new InstructionOperand(value));
    }

    public CodeAddress AddDefaultJump(Instruction jump)
    {
        Add(jump, new(new CodeAddress()));
        return Count - 1; // index of jump target address
    }

    #endregion
}
