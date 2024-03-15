using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace scripting.Generation;

public enum InstructionType : byte
{
    NoOp,
    Start, End, // Helps with jumps not going out of bounds

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
    #region Constructors

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

    #endregion Constructors

    #region Properties

    public readonly ushort Operand { get; }

    #endregion Properties
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
public class InstructionList : List<InstructionUnion>
{
    #region Constructors

    public InstructionList() : base() { }

    public InstructionList(int capacity) : base(capacity) { }

    #endregion Constructors

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
        AddRange(new InstructionUnion[2] { unionI, unionO });
    }

    public void Add(InstructionType type, InstructionOperand operand)
    {
        Add(new Instruction(type, InstructionFlags.Continuation), operand);
    }

    #endregion Add

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
        InsertRange(address, new InstructionUnion[2] { unionI, unionO });
    }

    public void Insert(CodeAddress address, InstructionType type, InstructionOperand operand)
    {
        Insert(address, new Instruction(type, InstructionFlags.Continuation), operand);
    }

    #endregion Insert

    #region Find

    public bool TryFind(Instruction instruction, out CodeAddress index)
    {
        for (CodeAddress i = 0; i < Count; i += this[i].instruction.Type.Size())
        {
            if (this[i].instruction == instruction)
            {
                index = i;
                return true;
            }
        }

        index = default;
        return false;
    }

    public bool TryFind(Instruction instruction, int depth, out CodeAddress index)
    {
        for (CodeAddress i = 0; i < Count; i += this[i].instruction.Type.Size())
        {
            if (this[i].instruction.Type == instruction.Type && depth-- == 0)
            {
                index = i;
                return true;
            }
        }

        index = default;
        return false;
    }

    #endregion Find

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

    #endregion Replace

    #region Remove

    public void RemoveAt(CodeAddress address)
    {
        base.RemoveAt(address);
    }

    public void RemoveRange(CodeAddress index, CodeAddress count)
    {
        base.RemoveRange(index, count);
    }

    #endregion Remove
}

/// <summary>
/// Represents an address in the Interpreter's Heap Memory.
/// </summary>
/// <param name="Address">Heap Memory address.</param>
public readonly record struct MemoryAddress(byte Address)
{
    #region Constants

    public const int SIZE = sizeof(byte);
    public const byte MAX_VALUE = byte.MaxValue;
    public const ushort CAPACITY = MAX_VALUE + 1;

    #endregion Constants

    #region Operators

    public static implicit operator MemoryAddress(byte pointer)
    {
        return new(pointer);
    }

    public static implicit operator MemoryAddress(int pointer)
    {
        byte address = (byte)pointer;
        if (address > MAX_VALUE)
        {
            throw new InterpreterException("Memory address overflow!");
        }
        return address;
    }

    public static explicit operator MemoryAddress(InstructionOperand pointer)
    {
        return pointer.Operand;
    }

    public static implicit operator byte(MemoryAddress address)
    {
        return address.Address;
    }

    #endregion Operators
}

/// <summary>
/// Represents an address of static program data.
/// </summary>
/// <param name="Address">Data address.</param>
public readonly record struct DataAddress(ushort Address)
{
    #region Constants

    public const int SIZE = sizeof(ushort);
    public const ushort MAX_VALUE = ushort.MaxValue;
    public const int CAPACITY = MAX_VALUE + 1;

    #endregion Constants

    #region Operators

    public static implicit operator DataAddress(ushort pointer)
    {
        return new(pointer);
    }

    public static implicit operator DataAddress(int pointer)
    {
        ushort address = (ushort)pointer;
        if (address > MAX_VALUE)
        {
            throw new InterpreterException("Data address overflow!");
        }
        return address;
    }

    public static explicit operator DataAddress(InstructionOperand pointer)
    {
        return pointer.Operand;
    }

    public static implicit operator ushort(DataAddress address)
    {
        return address.Address;
    }

    #endregion Operators
}

/// <summary>
/// Represents an Instruction address in the Program in which it is present.
/// </summary>
/// <param name="Address">Instruction address.</param>
public readonly record struct CodeAddress(ushort Address)
{
    #region Constants

    public const int SIZE = sizeof(ushort);
    public const ushort MAX_VALUE = ushort.MaxValue;

    #endregion Constants

    #region Operators

    public static implicit operator CodeAddress(ushort pointer)
    {
        return new(pointer);
    }

    public static implicit operator CodeAddress(int pointer)
    {
        ushort address = (ushort)pointer;
        if (address > MAX_VALUE)
        {
            throw new InterpreterException("Code address overflow!");
        }
        return address;
    }

    public static explicit operator CodeAddress(InstructionOperand pointer)
    {
        return pointer.Operand;
    }

    public static implicit operator ushort(CodeAddress address)
    {
        return address.Address;
    }

    public static explicit operator byte[](CodeAddress address)
    {
        return BitConverter.GetBytes(address.Address);
    }

    #endregion Operators
}

/// <summary>
/// Represents a number or boolean in the script.
/// </summary>
/// <param name="Value">Value of the Number.</param>
public readonly record struct Number(double Value)
{
    #region Constants

    public const int SIZE = sizeof(double);
    public const double ZERO = 0.0;
    public const double DEFAULT_X = ZERO;
    public const double DEFAULT_Y = 1.0;

    #endregion Constants

    #region Static Methods

    public static Number Parse(string s)
    {
        return Parse(s, new InterpreterException("Cannot parse number!"));
    }

    public static Number Parse(string s, uint line)
    {
        return Parse(s, new InterpreterException("Cannot parse number!", line));
    }

    private static Number Parse(string s, InterpreterException e)
    {
        if (double.TryParse(s, NumberStyles.Float, NumberFormatInfo.InvariantInfo,
            out double result))
        {
            return result;
        }

        throw e;
    }

    #endregion Static Methods

    #region Operators

    public static implicit operator Number(bool value)
    {
        return Convert.ToDouble(value);
    }

    public static implicit operator Number(double value)
    {
        return new(value);
    }

    public static explicit operator Number(Token token)
    {
        Debug.Assert(token.Base.Type == TokenType.Number);
        return Parse(token.Base.Symbol, token.Line);
    }

    public static implicit operator bool(Number number)
    {
        return number.Value != ZERO;
    }

    public static implicit operator double(Number number)
    {
        return number.Value;
    }

    public static Number operator |(Number left, Number right)
    {
        return (left != ZERO) | (right != ZERO);
    }

    public static Number operator &(Number left, Number right)
    {
        return (left != ZERO) & (right != ZERO);
    }

    public static Number operator !(Number number)
    {
        return number == ZERO;
    }

    #endregion Operators
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
/// Represents the data segment, used for storing numbers from the code.
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

/// <summary>
/// Represents a program consisting of executable Instructions.
/// </summary>
public class Program
{
    #region Fields

    private readonly InstructionUnion[] instructions;

    private readonly StaticData data;

    #endregion Fields

    #region Constructors

    /// <summary>
    /// Initializes the InstructionList so that the Interpreter can execute it in order.
    /// </summary>
    /// <param name="code">Contains a parsed TokenList that can be emitted to bytecode.</param>
    /// <param name="memoryMap">Maps identifiers to memory addresses.</param>
    /// <exception cref="InterpreterException">Thrown when emitting fails.</exception>
    public Program(TokenCode code, Dictionary<string, MemoryAddress> memoryMap)
    {
        InstructionList instructionList = new(code.Length)
        {
            InstructionType.Start
        };

        DataAddress dataIndex = 0;
        Dictionary<Number, DataAddress> numberMap = new();

        CodeAddress lastExprStart = 0;
        int depth = 0;

        for (int i = 0; i < code.Length; i++)
        {
            Token token = code[i];
            BaseToken baseToken = token.Base;
            string symbol = baseToken.Symbol;
            switch (baseToken.Type)
            {
                case TokenType.Number:
                    Number number = Number.Parse(symbol, token.Line);

                    // See if the number is already present before assigning a new index
                    if (!numberMap.TryGetValue(number, out DataAddress dAddress))
                    {
                        numberMap.Add(number, dAddress = dataIndex++);
                    }
                    instructionList.Add(InstructionType.LoadNumber, new(dAddress));
                    break;
                case TokenType.Parameter:
                case TokenType.Variable:
                    MemoryAddress mAddress = memoryMap[symbol];
                    instructionList.Add(InstructionType.Load, new(mAddress));
                    break;
                case TokenType.Input:
                    instructionList.Add(InstructionType.LoadIn);
                    break;
                case TokenType.Output:
                    instructionList.Add(InstructionType.LoadOut);
                    break;
                case TokenType.Constant:
                    instructionList.Add(OnConstant(symbol, token.Line));
                    break;
                case TokenType.Branch:
                    InstructionFlags flags = InstructionFlags.Continuation;
                    if (token.IsLoop()) flags |= InstructionFlags.IsLoop;

                    instructionList.Add(new Instruction(InstructionType.BranchMarker, flags), new(lastExprStart));
                    lastExprStart = instructionList.Count - 1;

                    depth++;
                    break;
                case TokenType.BranchEnd:
                    if (depth-- != 0)
                    {
                        Instruction marker = new(InstructionType.BranchMarker);
                        if (instructionList.TryFind(marker, depth, out CodeAddress index))
                        {
                            marker = instructionList[index].instruction;
                            if (marker.AllFlags(InstructionFlags.IsLoop))
                            {
                                InstructionOperand markerCondition = instructionList[index + 1].operand;
                                instructionList.Add(InstructionType.Jmp, markerCondition);
                            }

                            CodeAddress condition = instructionList.Count - 1;
                            instructionList.Replace(index, InstructionType.Jz, new(condition));
                            break;
                        }
                    }

                    throw new InterpreterException("Unexpected branch end!", token.Line);
                case TokenType.Assignment:
                    InstructionType modify = OnAssignment(symbol, token.Line);
                    bool isInline = modify.IsInline();

                    // MUTATES i, because we don't want to add this token again on the next iteration
                    BaseToken target = code[++i].Base;
                    if (target.Type == TokenType.Input)
                    {
                        SpecialAssignment(
                            instructionList, isInline,
                            InstructionType.LoadIn, modify, InstructionType.StoreIn);
                    }
                    else if (target.Type == TokenType.Output)
                    {
                        SpecialAssignment(
                            instructionList, isInline,
                            InstructionType.LoadOut, modify, InstructionType.StoreOut);
                    }
                    else
                    {
                        MemoryAddress address = memoryMap[target.Symbol];
                        if (isInline)
                        {
                            instructionList.Add(InstructionType.Load, new(address));
                            instructionList.Add(InstructionType.Swap);
                            instructionList.Add(modify);
                        }
                        instructionList.Add(InstructionType.Store, new(address));
                    }

                    lastExprStart = instructionList.Count - 1;
                    break;
                case TokenType.Arithmetic:
                    InstructionType arithmetic = OnArithmetic(symbol, token.Line);

                    // Attempt to convert [...LoadE, Pow,...] to [...Exp,...]
                    if (arithmetic == InstructionType.Pow && instructionList.Count > 1)
                    {
                        Index lastIndex;
                        if (instructionList[lastIndex = ^1].instruction.Type == InstructionType.LoadE)
                        {
                            instructionList.RemoveAt(lastIndex.Value);
                            instructionList.Add(InstructionType.Exp);
                            break;
                        }
                    }

                    instructionList.Add(arithmetic);
                    break;
                case TokenType.Comparison:
                    instructionList.Add(OnComparison(symbol, token.Line));
                    break;
                case TokenType.Function:
                    instructionList.Add(OnFunction(symbol, token.Line));
                    break;
                default:
                    throw new InterpreterException("Cannot emit token!", token.Line);
            }
        }

        if (depth != 0)
        {
            throw new InterpreterException("Branch mismatch!");
        }

        data = new(numberMap.Count);
        foreach((Number number, DataAddress dAddress) in numberMap)
        {
            data[dAddress] = number;
        }

        instructionList.Add(InstructionType.End);
        instructions = instructionList.ToArray();
    }

    #endregion Constructors

    #region Properties and Methods

    public int Length => instructions.Length;

    public InstructionUnion this[CodeAddress index] => instructions[index];

    public InstructionOperand GetOperandFromNext(ref CodeAddress c) => this[++c].operand;

    public Number GetData(DataAddress index) => data[index];

    private static void SpecialAssignment(
        InstructionList instructionList, bool isInline,
        InstructionType load, InstructionType modify, InstructionType store)
    {
        if (isInline)
        {
            instructionList.Add(load);
            instructionList.Add(InstructionType.Swap);
            instructionList.Add(modify);
        }
        instructionList.Add(store);
    }

    #endregion Properties and Methods

    #region Jump Tables

    private static InstructionType OnConstant(string symbol, uint line) => symbol switch
    {
        Tokens.CONST_E   => InstructionType.LoadE,
        Tokens.CONST_PI  => InstructionType.LoadPi,
        Tokens.CONST_TAU => InstructionType.LoadTau,
        Tokens.ZERO      => InstructionType.LoadZero,

        _ => throw new InterpreterException("Cannot emit constant!", line),
    };

    private static InstructionType OnAssignment(string symbol, uint line) => symbol switch
    {
        Tokens.ASSIGN => InstructionType.Store,
        Tokens.IADD   => InstructionType.Add,
        Tokens.ISUB   => InstructionType.Sub,
        Tokens.IMUL   => InstructionType.Mul,
        Tokens.IDIV   => InstructionType.Div,
        Tokens.IMOD   => InstructionType.Mod,
        Tokens.IPOW   => InstructionType.Pow,

        _ => throw new InterpreterException("Cannot emit assignment!", line),
    };

    private static InstructionType OnArithmetic(string symbol, uint line) => symbol switch
    {
        Tokens.ADD => InstructionType.Add,
        Tokens.SUB => InstructionType.Sub,
        Tokens.MUL => InstructionType.Mul,
        Tokens.DIV => InstructionType.Div,
        Tokens.MOD => InstructionType.Mod,
        Tokens.POW => InstructionType.Pow,

        _ => throw new InterpreterException("Cannot emit arithmetic!", line)
    };

    private static InstructionType OnComparison(string symbol, uint line) => symbol switch
    {
        Tokens.OR  => InstructionType.Or,
        Tokens.AND => InstructionType.And,
        Tokens.LT  => InstructionType.Lt,
        Tokens.GT  => InstructionType.Gt,
        Tokens.LE  => InstructionType.Le,
        Tokens.GE  => InstructionType.Ge,
        Tokens.EQ  => InstructionType.Eq,
        Tokens.NE  => InstructionType.Ne,
        Tokens.NOT => InstructionType.Not,

        _ => throw new InterpreterException("Cannot emit comparison!", line),
    };

    private static InstructionType OnFunction(string symbol, uint line) => symbol switch
    {
        Tokens.ABS       => InstructionType.Abs,
        Tokens.SIGN      => InstructionType.Sign,
        Tokens.COPY_SIGN => InstructionType.CopySign,

        Tokens.ROUND => InstructionType.Round,
        Tokens.TRUNC => InstructionType.Trunc,
        Tokens.CEIL  => InstructionType.Ceil,
        Tokens.FLOOR => InstructionType.Floor,
        Tokens.CLAMP => InstructionType.Clamp,

        Tokens.MIN           => InstructionType.Min,
        Tokens.MAX           => InstructionType.Max,
        Tokens.MIN_MAGNITUDE => InstructionType.MinM,
        Tokens.MAX_MAGNITUDE => InstructionType.MaxM,

        Tokens.SQRT => InstructionType.Sqrt,
        Tokens.CBRT => InstructionType.Cbrt,

        Tokens.LOG   => InstructionType.Log,
        Tokens.LOG2  => InstructionType.Log2,
        Tokens.LOG10 => InstructionType.Log10,
        Tokens.LOGN  => InstructionType.LogN,

        Tokens.SIN   => InstructionType.Sin,
        Tokens.SINH  => InstructionType.Sinh,
        Tokens.ASIN  => InstructionType.Asin,
        Tokens.ASINH => InstructionType.Asinh,

        Tokens.COS   => InstructionType.Cos,
        Tokens.COSH  => InstructionType.Cosh,
        Tokens.ACOS  => InstructionType.Acos,
        Tokens.ACOSH => InstructionType.Acosh,

        Tokens.TAN   => InstructionType.Tan,
        Tokens.TANH  => InstructionType.Tanh,
        Tokens.ATAN  => InstructionType.Atan,
        Tokens.ATANH => InstructionType.Atanh,
        Tokens.ATAN2 => InstructionType.Atan2,

        Tokens.FUSED_MULTIPLY_ADD => InstructionType.FusedMultiplyAdd,
        Tokens.SCALE_B            => InstructionType.ScaleB,

        _ => throw new InterpreterException("Cannot emit function!", line),
    };

    #endregion Jump Tables
}
