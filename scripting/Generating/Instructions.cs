using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace scripting.Generating;

/// <summary>
/// Enumerates all types of instructions (for a stack machine).
/// </summary>
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
/// Provides helper/extension methods.
/// </summary>
public static class Instructions
{
    /// <summary>
    /// Gets the length of the address that follows this instruction.
    /// </summary>
    /// <param name="type">The type of the instruction</param>
    /// <returns>Length of subsequent address in bytes</returns>
    public static int AddressLength(this InstructionType type) => type switch
    {
        InstructionType.Load => MemoryAddress.SIZE,
        InstructionType.Store => MemoryAddress.SIZE,

        InstructionType.LoadNumber => DataAddress.SIZE,

        InstructionType.Jmp => CodeAddress.SIZE,
        InstructionType.Jz => CodeAddress.SIZE,

        _ => 0,
    };

    /// <summary>
    /// Looks up if this instruction is a branch/jump instruction.
    /// </summary>
    /// <param name="type">The type of the instruction</param>
    /// <returns>Whether this instruction is a jump</returns>
    public static bool IsBranch(this InstructionType type) => type switch
    {
        InstructionType.Jmp => true,
        InstructionType.Jz => true,

        _ => false,
    };

    /// <summary>
    /// Looks up if this instruction is an inline assignment (i.e. 'composite' assignment).
    /// </summary>
    /// <param name="type">The type of the instruction</param>
    /// <returns>Whether this instruction is an inline assignment operation</returns>
    public static bool IsInline(this InstructionType type) => type != InstructionType.Store;
}
