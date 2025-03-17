namespace userspace_backend.ScriptingLanguage.Compiler;

/// <summary>
/// Enumerates all kinds of instructions (for a stack machine).
/// </summary>
public enum InstructionKind : byte
{
    NoOp,
    Start, End, // Helps with jumps not going out of bounds (in an otherwise correct program)
    Return, // Early return 

    // Load/Store (TOS = Top Of Stack)
    LoadNumber,                          // Loads a number from data.
    LoadIn, StoreIn,                     // Gets/Sets the input register (x), to/from TOS.
    LoadOut, StoreOut,                   // Gets/Sets the output register (y), to/from TOS.
    LoadPersistent, StorePersistent,     // Gets/Sets an address in persistent memory, to/from TOS.
    LoadImpersistent, StoreImpersistent, // Gets/Sets an address in impersistent memory, to/from TOS.
    LoadStack, StoreStack,               // Gets/Sets a certain index of the stack.
    Swap,                                // Swaps the top two stack elements.

    // Constant,
    // Pushes a constant to the stack.
    LoadZero,
    LoadCapacity,
    LoadE, LoadPi, LoadTau,

    // Branch,
    // Evaluates the TOS and jumps/skips to the next branch end marker if zero (Jz).
    // The jump itself can be unconditional (Jmp) instead, to implement loops (Jmp backwards).
    Jmp, Jz,

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
    Call, // user-defined

    // Take arguments from the stack and give a transformed version back.
    Abs, Sign, CopySign,
    Round, Trunc, Floor, Ceil, Clamp,
    Min, Max, MinM, MaxM,
    Sqrt, Cbrt,
    Log, Log2, Log10, LogB, ILogB,
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
    /// <param name="kind">The kind of instruction.</param>
    /// <returns>Length of subsequent address in bytes.</returns>
    public static int AddressLength(this InstructionKind kind) => kind switch
    {
        InstructionKind.LoadPersistent => sizeof(MemoryAddress),
        InstructionKind.StorePersistent => sizeof(MemoryAddress),
        InstructionKind.LoadImpersistent => sizeof(MemoryAddress),
        InstructionKind.StoreImpersistent => sizeof(MemoryAddress),

        InstructionKind.Call => sizeof(MemoryAddress),

        InstructionKind.LoadNumber => sizeof(DataAddress),

        InstructionKind.LoadStack => sizeof(StackAddress),
        InstructionKind.StoreStack => sizeof(StackAddress),

        InstructionKind.Jmp => sizeof(CodeAddress),
        InstructionKind.Jz => sizeof(CodeAddress),

        _ => 0
    };

    /// <summary>
    /// Looks up if this instruction is a branch/jump instruction.
    /// </summary>
    /// <param name="kind">The kind of instruction.</param>
    /// <returns>Whether this instruction is a jump.</returns>
    public static bool IsBranch(this InstructionKind kind) => kind switch
    {
        InstructionKind.Jmp => true,
        InstructionKind.Jz => true,

        _ => false
    };
}
