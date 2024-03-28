using scripting.Script;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace scripting.Interpretation;

public enum InstructionType : byte
{
    NoOp,
    Start, End, // Helps with jumps not going out of bounds
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
    BranchMarker,

    // Constant,
    // Pushes a constant to the stack.
    LoadE, LoadPi, LoadTau, LoadZero,

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
    Round, Trunc, Ceil, Floor, Clamp,
    Min, Max, MinM, MaxM,
    Sqrt, Cbrt,
    Log, Log2, Log10, LogN,
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
    static Instructions()
    {
        Debug.Assert(InstructionType.Jz.Size() == InstructionType.BranchMarker.Size());
    }

    public static byte Size(this InstructionType type) => type switch
    {
        InstructionType.Load => 2,
        InstructionType.Store => 2,
        InstructionType.LoadNumber => 2,

        InstructionType.Jz => 2,
        InstructionType.Jmp => 2,
        InstructionType.BranchMarker => 2,

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
    /// Whether this instruction is expecting to be replaced by a looping body.
    /// </summary>
    IsLoop = 1 << 1,
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

    #region Insert

    public void Insert(CodeAddress address, Instruction instruction)
    {
        Debug.Assert(instruction.Type.Size() == 1);

        InstructionUnion unionI = new()
        {
            instruction = instruction
        };
        Insert(address, unionI);
    }

    public void Insert(CodeAddress address, InstructionType type)
    {
        Insert(address, new Instruction(type));
    }

    public void Insert(CodeAddress address, Instruction instruction, InstructionOperand operand)
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
        InsertRange(address, [unionI, unionO]);
    }

    public void Insert(CodeAddress address, InstructionType type, InstructionOperand operand)
    {
        Insert(address, new Instruction(type, InstructionFlags.Continuation), operand);
    }

    #endregion

    #region Find

    public bool TryFind(Instruction instruction, out CodeAddress index)
    {
        for (CodeAddress c = 0; c < Count; c += GetOffset(c))
        {
            if (this[c].instruction == instruction)
            {
                index = c;
                return true;
            }
        }

        index = default;
        return false;
    }

    public bool TryFind(Instruction instruction, int depth, out CodeAddress index)
    {
        for (CodeAddress c = 0; c < Count; c += GetOffset(c))
        {
            if (this[c].instruction.Type == instruction.Type && depth-- == 0)
            {
                index = c;
                return true;
            }
        }

        index = default;
        return false;
    }

    #endregion

    #region Replace

    public void Replace(CodeAddress instructionIndex, Instruction instruction, InstructionOperand operand)
    {
        CodeAddress operandIndex = instructionIndex + 1;
        Debug.Assert(operandIndex < Count);

        InstructionUnion instructionUnion = this[instructionIndex];
        InstructionUnion operandUnion = this[operandIndex];

        instructionUnion.instruction = instruction;
        operandUnion.operand = operand;

        this[instructionIndex] = instructionUnion;
        this[operandIndex] = operandUnion;
    }

    public void Replace(CodeAddress instructionIndex, InstructionType type, InstructionOperand operand)
    {
        Replace(instructionIndex, new Instruction(type), operand);
    }

    #endregion

    #region Remove

    public void RemoveAt(CodeAddress address)
    {
        base.RemoveAt(address);
    }

    public void RemoveRange(CodeAddress index, CodeAddress count)
    {
        base.RemoveRange(index, count);
    }

    #endregion

    #region Helpers

    private CodeAddress GetOffset(CodeAddress c) => this[c].instruction.Type.Size();

    #endregion
}

/// <summary>
/// Represents an address in the Interpreter's Heap Memory.
/// </summary>
/// <param name="Address">Heap Memory address.</param>
public readonly record struct MemoryAddress(byte Address)
{
    #region Constants

    public const int SIZE = sizeof (byte);

    public const byte MAX_VALUE = byte.MaxValue;
    public const int CAPACITY = MAX_VALUE + 1;

    #endregion

    #region Operators

    public static implicit operator MemoryAddress(byte pointer)
    {
        return new(pointer);
    }

    public static implicit operator MemoryAddress(int pointer)
    {
        if (pointer > MAX_VALUE)
        {
            throw new InterpreterException("Memory address overflow!");
        }
        return (byte)pointer;
    }

    public static explicit operator MemoryAddress(InstructionOperand pointer)
    {
        return pointer.Operand;
    }

    public static implicit operator byte(MemoryAddress address)
    {
        return address.Address;
    }

    #endregion
}

/// <summary>
/// Represents an address of static program data.
/// </summary>
/// <param name="Address">Data address.</param>
public readonly record struct DataAddress(ushort Address)
{
    #region Constants

    public const int SIZE = sizeof (ushort);

    public const ushort MAX_VALUE = ushort.MaxValue;
    public const int CAPACITY = MAX_VALUE + 1;

    #endregion

    #region Operators

    public static implicit operator DataAddress(ushort pointer)
    {
        return new(pointer);
    }

    public static implicit operator DataAddress(int pointer)
    {
        if (pointer > MAX_VALUE)
        {
            throw new InterpreterException("Data address overflow!");
        }
        return (ushort)pointer;
    }

    public static explicit operator DataAddress(InstructionOperand pointer)
    {
        return pointer.Operand;
    }

    public static implicit operator ushort(DataAddress address)
    {
        return address.Address;
    }

    #endregion
}

/// <summary>
/// Represents an Instruction address in the Program in which it is present.
/// </summary>
/// <param name="Address">Instruction address.</param>
public readonly record struct CodeAddress(ushort Address)
{
    #region Constants

    public const int SIZE = sizeof (ushort);

    public const ushort MAX_VALUE = ushort.MaxValue;
    public const int CAPACITY = MAX_VALUE + 1;

    #endregion

    #region Operators

    public static implicit operator CodeAddress(ushort pointer)
    {
        return new(pointer);
    }

    public static implicit operator CodeAddress(int pointer)
    {
        if (pointer > MAX_VALUE)
        {
            throw new InterpreterException("Code address overflow!");
        }
        return (ushort)pointer;
    }

    public static explicit operator CodeAddress(InstructionOperand pointer)
    {
        return pointer.Operand;
    }

    public static implicit operator ushort(CodeAddress address)
    {
        return address.Address;
    }

    #endregion
}

/// <summary>
/// Represents a heap of memory used by:
/// Parameters (indices [0, 7])
/// Variables (indices after that, as many as needed)
/// </summary>
public class MemoryHeap
{
    private readonly Number[] Memory;

    public MemoryHeap(int capacity)
    {
        if (capacity > MemoryAddress.CAPACITY)
        {
            throw new InterpreterException("MemoryHeap capacity overflow!");
        }
        
        Memory = new Number[capacity];
    }

    public Number this[MemoryAddress address]
    {
        get { return Memory[address]; }
        set { Memory[address] = value; }
    }

    public void CopyFrom(MemoryHeap other)
    {
        Debug.Assert(Memory.Length == other.Memory.Length);
        other.Memory.CopyTo(Memory, 0);
    }
}

/// <summary>
/// Represents the data cache, used for storing number literals from the code.
/// </summary>
public class StaticData
{
    private readonly Number[] Data;

    public StaticData(int capacity)
    {
        if (capacity > DataAddress.CAPACITY)
        {
            throw new InterpreterException("StaticData capacity overflow!");
        }

        Data = new Number[capacity];
    }

    public Number this[DataAddress address]
    {
        get { return Data[address]; }
        set { Data[address] = value; }
    }
}
