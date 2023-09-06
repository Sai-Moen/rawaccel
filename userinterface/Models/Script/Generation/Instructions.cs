using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace userinterface.Models.Script.Generation
{
    public enum InstructionType : byte
    {
        Start, End,     // Helps with jumps not going out of bounds

        Load, Store,        // Gets or Sets an Address in 'virtual' heap, to/from TOS.
        LoadIn, StoreIn,    // Gets or Sets the input register (x), to/from TOS.
        LoadOut, StoreOut,  // Gets or Sets the output register (y), to/from TOS.
        LoadNumber,         // Loads a number.
        Swap,               // Swaps the top two stack elements.

        // Branch,
        // Evaluates the TOS and jumps/skips to the next branch end marker if zero (Jz).
        // The jump itself can be unconditional (Jmp) instead, to implement loops (Jmp backwards).
        Jmp, Jz,

        LoadE, LoadPi, LoadTau, LoadZero,   // Loads a constant to TOS.

        // Operator,
        // does an operation on the second and first Stack item respectively,
        // pushes the result onto the stack if the next instruction is not another operator.
        Add, Sub, Mul, Div, Mod, Exp, ExpE,

        // Comparison,
        // returns, for some condition, 1.0 when true, 0.0 when false.
        Or, And,
        Lt, Gt, Le, Ge,
        Eq, Ne, Not,

        // Function,
        // Take the TOS and Push a transformed version back.
        Abs, Sqrt, Cbrt,
        Round, Trunc, Ceil, Floor,
        Log, Log2, Log10,
        Sin, Sinh, Asin, Asinh,
        Cos, Cosh, Acos, Acosh,
        Tan, Tanh, Atan, Atanh,

        // Leave this at the bottom of the enum for obvious reasons.
        Count
    }

    /// <summary>
    /// Represents the context needed by a BranchEnd Token to determine the emitted instruction(s).
    /// </summary>
    /// <param name="IsLoop">Whether or not this context refers to a looping body.</param>
    /// <param name="Condition">The instruction address where the Branch condition starts.</param>
    /// <param name="Insert">The instruction address where a conditional jump should be inserted.</param>
    public readonly record struct BranchContext(bool IsLoop, CodeAddress Condition, CodeAddress Insert);

    /// <summary>
    /// Represents an address in the Interpreter's Heap Memory.
    /// </summary>
    /// <param name="Address">Heap Memory address.</param>
    public readonly record struct MemoryAddress(byte Address)
    {
        public const int Size = sizeof(byte);
        public const byte MaxValue = byte.MaxValue;

        public static implicit operator MemoryAddress(byte pointer)
        {
            return new(pointer);
        }

        public static implicit operator MemoryAddress(int pointer)
        {
            byte address = (byte)pointer;
            if (address > MaxValue)
            {
                throw new InterpreterException("Memory address overflow!");
            }
            return address;
        }

        public static explicit operator MemoryAddress(Instruction pointer)
        {
            return pointer.Data[0];
        }

        public static implicit operator byte(MemoryAddress address)
        {
            return address.Address;
        }

        public static explicit operator byte[](MemoryAddress address)
        {
            return new byte[1]{ address.Address };
        }
    }

    /// <summary>
    /// Represents an Instruction address in the Program in which it is present.
    /// </summary>
    /// <param name="Address">Instruction address.</param>
    public readonly record struct CodeAddress(ushort Address)
    {
        public const int Size = sizeof(ushort);
        public const ushort MaxValue = ushort.MaxValue;

        public static implicit operator CodeAddress(ushort pointer)
        {
            return new(pointer);
        }

        public static implicit operator CodeAddress(int pointer)
        {
            ushort address = (ushort)pointer;
            if (address > MaxValue)
            {
                throw new InterpreterException("Code address overflow!");
            }
            return address;
        }

        public static explicit operator CodeAddress(Instruction pointer)
        {
            byte[] bytes = new byte[Size];
            pointer.CopyTo(ref bytes);
            return BitConverter.ToUInt16(bytes);
        }

        public static implicit operator ushort(CodeAddress address)
        {
            return address.Address;
        }

        public static explicit operator byte[](CodeAddress address)
        {
            return BitConverter.GetBytes(address.Address);
        }
    }

    /// <summary>
    /// Represents a number or boolean in the script.
    /// </summary>
    /// <param name="Value">Value of the Number.</param>
    public readonly record struct Number(double Value)
    {
        public const int Size = sizeof(double);
        public const double Zero = 0;
        public const double DefaultX = Zero;
        public const double DefaultY = 1;

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
            return Parse(token.Base.Symbol, token.Line);
        }

        public static explicit operator Number(Instruction value)
        {
            byte[] bytes = new byte[Size];
            value.CopyTo(ref bytes);
            return BitConverter.ToDouble(bytes);
        }

        public static implicit operator bool(Number number)
        {
            return number.Value != Zero;
        }

        public static implicit operator double(Number number)
        {
            return number.Value;
        }

        public static explicit operator byte[](Number number)
        {
            return BitConverter.GetBytes(number.Value);
        }

        public static Number operator |(Number left, Number right)
        {
            return (left != Zero) | (right != Zero);
        }

        public static Number operator &(Number left, Number right)
        {
            return (left != Zero) & (right != Zero);
        }

        public static Number operator !(Number number)
        {
            return number == Zero;
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

        public static Number Parse(string s)
        {
            return Parse(s, new InterpreterException("Cannot parse number!"));
        }

        public static Number Parse(string s, uint line)
        {
            return Parse(s, new InterpreterException(line, "Cannot parse number!"));
        }
    }

    /// <summary>
    /// Represents an Instruction that can be executed by the Interpreter.
    /// </summary>
    public readonly record struct Instruction(InstructionType Type, byte[] Data)
    {
        public static implicit operator Instruction(InstructionType type)
        {
            return new(type, new byte[type.SizeOf()]);
        }

        public static implicit operator InstructionType(Instruction instruction)
        {
            return instruction.Type;
        }

        public void CopyFrom(byte[] bytes)
        {
            bytes.CopyTo(Data, 0);
        }

        public void CopyTo(ref byte[] bytes)
        {
            Span<byte> span = new(Data, 0, Data.Length);
            span.CopyTo(bytes);
        }
    }

    /// <summary>
    /// Provides extension methods for Instruction and InstructionType.
    /// </summary>
    public static class Instructions
    {
        public static int SizeOf(this InstructionType type)
        {
            return type switch
            {
                InstructionType.LoadNumber  => Number.Size,

                InstructionType.Jmp         => CodeAddress.Size,
                InstructionType.Jz          => CodeAddress.Size,

                InstructionType.Load        => MemoryAddress.Size,
                InstructionType.Store       => MemoryAddress.Size,

                _ => 0,
            };
        }
    }

    /// <summary>
    /// Represents a program consisting of executable Instructions.
    /// </summary>
    public class Program
    {
        #region Constructors

        /// <summary>
        /// Initializes the InstructionList so that the Interpreter can execute it in order.
        /// </summary>
        /// <param name="code">Contains a parsed TokenList that can be emitted to bytecode.</param>
        /// <param name="map">Maps identifiers to memory addresses.</param>
        /// <exception cref="InterpreterException">Thrown when emitting fails.</exception>
        public Program(Expression code, MemoryMap map)
        {
            // Allocates mostly enough, but not guaranteed, therefore List
            Instructions = new(code.Tokens.Length);
            Instructions.AddInstruction(InstructionType.Start);

            Stack<BranchContext> stack = new();

            int lastExprStart = 0;

            for (int i = 0; i < code.Tokens.Length; i++)
            {
                Token token = code.Tokens[i];
                string symbol = token.Base.Symbol;
                switch (token.Base.Type)
                {
                    case TokenType.Number:
                        Number number = Number.Parse(symbol, token.Line);
                        Instructions.AddInstruction(InstructionType.LoadNumber, (byte[])number);
                        break;
                    case TokenType.Parameter:
                    case TokenType.Variable:
                        MemoryAddress mAddress = map[symbol];
                        Instructions.AddInstruction(InstructionType.Load, (byte[])mAddress);
                        break;
                    case TokenType.Input:
                        Instructions.AddInstruction(InstructionType.LoadIn);
                        break;
                    case TokenType.Output:
                        Instructions.AddInstruction(InstructionType.LoadOut);
                        break;
                    case TokenType.Constant:
                        Instructions.AddInstruction(OnConstant(symbol, token.Line));
                        break;
                    case TokenType.Branch:
                        BranchContext context = new(
                            token.IsLoop(),
                            lastExprStart + stack.Count,
                            Instructions.Count);
                        stack.Push(context);
                        lastExprStart = Instructions.Count - 1;
                        break;
                    case TokenType.BranchEnd:
                        if (stack.TryPop(out BranchContext ctx))
                        {
                            if (ctx.IsLoop)
                            {
                                Instructions.AddInstruction(InstructionType.Jmp, (byte[])ctx.Condition);
                            }
                            CodeAddress cAddress = Instructions.Count + stack.Count;
                            Instructions.InsertInstruction(ctx.Insert, InstructionType.Jz, (byte[])cAddress);
                            break;
                        }

                        throw new InterpreterException(token.Line, "Unexpected branch end!");
                    case TokenType.Assignment:
                        InstructionType type = OnAssignment(symbol, token.Line);

                        // MUTATES i, because we don't want to add this token again on the next iteration
                        BaseToken target = code.Tokens[++i].Base;
                        if (target.Type == TokenType.Input)
                        {
                            OnAssignment(InstructionType.LoadIn, type, InstructionType.StoreIn,
                                Array.Empty<byte>());
                        }
                        else if (target.Type == TokenType.Output)
                        {
                            OnAssignment(InstructionType.LoadOut, type, InstructionType.StoreOut,
                                Array.Empty<byte>());
                        }
                        else
                        {
                            MemoryAddress address = map[target.Symbol];
                            byte[] pointer = (byte[])address;
                            OnAssignment(InstructionType.Load, type, InstructionType.Store, pointer);
                        }
                        lastExprStart = Instructions.Count - 1;
                        break;
                    case TokenType.Arithmetic:
                        OnArithmetic(symbol, token.Line);
                        break;
                    case TokenType.Comparison:
                        Instructions.AddInstruction(OnComparison(symbol, token.Line));
                        break;
                    case TokenType.Function:
                        Instructions.AddInstruction(OnFunction(symbol, token.Line));
                        break;
                    default:
                        throw new InterpreterException(token.Line, "Cannot emit token!");
                }
            }

            if (stack.Count != 0)
            {
                throw new InterpreterException("Branch mismatch!");
            }

            Instructions.AddInstruction(InstructionType.End);
            Instructions.TrimExcess();
        }

        #endregion Constructors

        #region Properties

        public InstructionList Instructions { get; }

        #endregion Properties

        #region Jump Tables

        private static InstructionType OnConstant(string symbol, uint line)
        {
            return symbol switch
            {
                Tokens.CONST_E => InstructionType.LoadE,
                Tokens.CONST_PI => InstructionType.LoadPi,
                Tokens.CONST_TAU => InstructionType.LoadTau,
                Tokens.ZERO => InstructionType.LoadZero,

                _ => throw new InterpreterException(line, "Cannot emit constant!"),
            };
        }

        private static InstructionType OnAssignment(string symbol, uint line)
        {
            return symbol switch
            {
                Tokens.ASSIGN => InstructionType.Store,
                Tokens.IADD => InstructionType.Add,
                Tokens.ISUB => InstructionType.Sub,
                Tokens.IMUL => InstructionType.Mul,
                Tokens.IDIV => InstructionType.Div,
                Tokens.IMOD => InstructionType.Mod,
                Tokens.IEXP => InstructionType.Exp,

                _ => throw new InterpreterException(line, "Cannot emit assignment!"),
            };
        }

        private void OnAssignment(
            InstructionType load,
            InstructionType modify,
            InstructionType store,
            byte[] pointer)
        {
            bool isInline = modify != InstructionType.Store;
            if (isInline)
            {
                Instructions.AddInstruction(load, pointer);
                Instructions.AddInstruction(InstructionType.Swap);
                Instructions.AddInstruction(modify);
            }
            
            Instructions.AddInstruction(store, pointer);
        }

        private void OnArithmetic(string symbol, uint line)
        {
            switch (symbol)
            {
                case Tokens.ADD:
                    Instructions.AddInstruction(InstructionType.Add);
                    break;
                case Tokens.SUB:
                    Instructions.AddInstruction(InstructionType.Sub);
                    break;
                case Tokens.MUL:
                    Instructions.AddInstruction(InstructionType.Mul);
                    break;
                case Tokens.DIV:
                    Instructions.AddInstruction(InstructionType.Div);
                    break;
                case Tokens.MOD:
                    Instructions.AddInstruction(InstructionType.Mod);
                    break;
                case Tokens.EXP:
                    // Try to convert E^ -> Exp()
                    int lastIndex = Instructions.Count - 1;
                    if (Instructions.Count > 0 &&
                        Instructions[lastIndex] == InstructionType.LoadE)
                    {
                        Instructions.RemoveAt(lastIndex);
                        Instructions.AddInstruction(InstructionType.ExpE);
                    }
                    else
                    {
                        Instructions.AddInstruction(InstructionType.Exp);
                    }

                    break;
                default:
                    throw new InterpreterException(line, "Cannot emit arithmetic!");
            }
        }

        private static InstructionType OnComparison(string symbol, uint line)
        {
            return symbol switch
            {
                Tokens.OR => InstructionType.Or,
                Tokens.AND => InstructionType.And,
                Tokens.LT => InstructionType.Lt,
                Tokens.GT => InstructionType.Gt,
                Tokens.LE => InstructionType.Le,
                Tokens.GE => InstructionType.Ge,
                Tokens.EQ => InstructionType.Eq,
                Tokens.NE => InstructionType.Ne,
                Tokens.NOT => InstructionType.Not,

                _ => throw new InterpreterException(line, "Cannot emit comparison!"),
            };
        }

        private static InstructionType OnFunction(string symbol, uint line)
        {
            return symbol switch
            {
                Tokens.ABS      => InstructionType.Abs,
                Tokens.SQRT     => InstructionType.Sqrt,
                Tokens.CBRT     => InstructionType.Cbrt,
                Tokens.ROUND    => InstructionType.Round,
                Tokens.TRUNC    => InstructionType.Trunc,
                Tokens.CEIL     => InstructionType.Ceil,
                Tokens.FLOOR    => InstructionType.Floor,
                Tokens.LOG      => InstructionType.Log,
                Tokens.LOG2     => InstructionType.Log2,
                Tokens.LOG10    => InstructionType.Log10,
                Tokens.SIN      => InstructionType.Sin,
                Tokens.SINH     => InstructionType.Sinh,
                Tokens.ASIN     => InstructionType.Asin,
                Tokens.ASINH    => InstructionType.Asinh,
                Tokens.COS      => InstructionType.Cos,
                Tokens.COSH     => InstructionType.Cosh,
                Tokens.ACOS     => InstructionType.Acos,
                Tokens.ACOSH    => InstructionType.Acosh,
                Tokens.TAN      => InstructionType.Tan,
                Tokens.TANH     => InstructionType.Tanh,
                Tokens.ATAN     => InstructionType.Atan,
                Tokens.ATANH    => InstructionType.Atanh,

                _ => throw new InterpreterException(line, "Cannot emit function!"),
            };
        }

        #endregion Jump Tables
    }

    public class InstructionList : List<Instruction>
    {
        public InstructionList() : base() { }

        public InstructionList(int capacity) : base(capacity) { }

        public void AddInstruction(InstructionType type)
        {
            Debug.Assert(type.SizeOf() == 0);

            Add(type);
        }

        public void AddInstruction(InstructionType type, byte[] data)
        {
            Instruction instruction = type;
            instruction.CopyFrom(data);
            Add(instruction);
        }

        public void InsertInstruction(CodeAddress address, InstructionType type, byte[] data)
        {
            Instruction instruction = type;
            instruction.CopyFrom(data);
            Insert(address, instruction);
        }
    }

    public class MemoryHeap
    {
        private readonly Number[] Mem;

        public MemoryHeap(int capacity)
        {
            Mem = new Number[capacity];
        }

        public double this[MemoryAddress address]
        {
            get { return Mem[address]; }
            set { Mem[address] = value; }
        }

        public void CopyFrom(MemoryHeap other)
        {
            other.Mem.CopyTo(Mem, 0);
        }
    }

    public class MemoryMap : Dictionary<string, MemoryAddress>, IDictionary<string, MemoryAddress>
    { }

    public class ProgramStack : Stack<Number>
    { }
}
