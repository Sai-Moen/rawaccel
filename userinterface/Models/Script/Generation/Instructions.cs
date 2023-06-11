using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

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
        Count,
    }

    public readonly struct Instruction
    {
        public const int Size = sizeof(byte);

        private readonly byte[] ByteCode;

        public Instruction(InstructionType type)
        {
            ByteCode = new byte[type.Size()];
            ByteCode[0] = type.ToByte();
        }

        public InstructionType GrabType()
        {
            Debug.Assert(ByteCode[0] < InstructionType.Count.ToByte());
            return (InstructionType)ByteCode[0];
        }

        public byte this[int index]
        {
            get { return ByteCode[index]; }
        }

        public void CopyFrom(byte[] bytes)
        {
            bytes.CopyTo(ByteCode, 1);
        }

        public void CopyTo(ref byte[] bytes)
        {
            Span<byte> span = new(ByteCode, 1, ByteCode.Length - 1);
            span.CopyTo(bytes);
        }
    }

    public readonly struct MemoryAddress
    {
        public const byte MaxValue = byte.MaxValue;

        public const int Size = sizeof(byte);

        public MemoryAddress(byte address)
        {
            Address = address;
        }

        public MemoryAddress(int address)
        {
            if (address > MaxValue)
            {
                throw new InterpreterException("Memory address overflow!");
            }

            Address = (byte)address;
        }

        public MemoryAddress(Instruction address)
        {
            Address = address[1];
        }

        public byte Address { get; }

        public byte[] GetBytes()
        {
            return new byte[1]{ Address };
        }
    }

    public readonly struct CodeAddress
    {
        public const ushort MaxValue = ushort.MaxValue;

        public const int Size = sizeof(ushort);

        public CodeAddress(int address)
        {
            if (address > MaxValue)
            {
                throw new InterpreterException("Code address overflow!");
            }

            Address = (ushort)address;
        }

        public CodeAddress(Instruction address)
        {
            byte[] bytes = new byte[Size];
            address.CopyTo(ref bytes);
            Address = BitConverter.ToUInt16(bytes);
        }

        public ushort Address { get; }

        public byte[] GetBytes()
        {
            return BitConverter.GetBytes(Address);
        }
    }

    public readonly struct Number
    {
        public const int Size = sizeof(double);

        public static readonly Number Zero = new(0);

        public Number(double value)
        {
            Value = value;
        }

        public Number(Instruction number)
        {
            byte[] bytes = new byte[Size];
            number.CopyTo(ref bytes);
            Value = BitConverter.ToDouble(bytes);
        }

        public double Value { get; }

        public byte[] GetBytes()
        {
            return BitConverter.GetBytes(Value);
        }
    }

    public readonly struct BranchContext
    {
        public BranchContext(int index, CodeAddress condition, CodeAddress insert)
        {
            Index = index;
            Condition = condition;
            Insert = insert;
        }

        public int Index { get; }

        public CodeAddress Condition { get; }

        public CodeAddress Insert { get; }
    }

    public static class Instructions
    {
        static Instructions()
        {
            // Arguments are reversed because of the Stack Pop order

            Table[InstructionType.LoadE.ToByte()]       = (_, _) => Math.E;
            Table[InstructionType.LoadPi.ToByte()]      = (_, _) => Math.PI;
            Table[InstructionType.LoadTau.ToByte()]     = (_, _) => Math.Tau;
            Table[InstructionType.LoadZero.ToByte()]    = (_, _) => 0;

            Table[InstructionType.Add.ToByte()]         = (y, x) => x + y;
            Table[InstructionType.Sub.ToByte()]         = (y, x) => x - y;
            Table[InstructionType.Mul.ToByte()]         = (y, x) => x * y;
            Table[InstructionType.Div.ToByte()]         = (y, x) => x / y;
            Table[InstructionType.Mod.ToByte()]         = (y, x) => x % y;
            Table[InstructionType.Exp.ToByte()]         = (y, x) => Math.Pow(x, y);
            Table[InstructionType.ExpE.ToByte()]        = (y, _) => Math.Exp(y);

            Table[InstructionType.Or.ToByte()]          = (y, x) => Convert.ToDouble((x != 0) | (y != 0));
            Table[InstructionType.And.ToByte()]         = (y, x) => Convert.ToDouble((x != 0) & (y != 0));
            Table[InstructionType.Lt.ToByte()]          = (y, x) => Convert.ToDouble(x < y);
            Table[InstructionType.Gt.ToByte()]          = (y, x) => Convert.ToDouble(x > y);
            Table[InstructionType.Le.ToByte()]          = (y, x) => Convert.ToDouble(x <= y);
            Table[InstructionType.Ge.ToByte()]          = (y, x) => Convert.ToDouble(x >= y);
            Table[InstructionType.Eq.ToByte()]          = (y, x) => Convert.ToDouble(x == y);
            Table[InstructionType.Ne.ToByte()]          = (y, x) => Convert.ToDouble(x != y);
            Table[InstructionType.Not.ToByte()]         = (y, _) => Convert.ToDouble(y == 0);

            Table[InstructionType.Abs.ToByte()]         = (y, _) => Math.Abs(y);
            Table[InstructionType.Sqrt.ToByte()]        = (y, _) => Math.Sqrt(y);
            Table[InstructionType.Cbrt.ToByte()]        = (y, _) => Math.Cbrt(y);

            Table[InstructionType.Round.ToByte()]       = (y, _) => Math.Round(y);
            Table[InstructionType.Trunc.ToByte()]       = (y, _) => Math.Truncate(y);
            Table[InstructionType.Ceil.ToByte()]        = (y, _) => Math.Ceiling(y);
            Table[InstructionType.Floor.ToByte()]       = (y, _) => Math.Floor(y);

            Table[InstructionType.Log.ToByte()]         = (y, _) => Math.Log(y);
            Table[InstructionType.Log2.ToByte()]        = (y, _) => Math.Log2(y);
            Table[InstructionType.Log10.ToByte()]       = (y, _) => Math.Log10(y);

            Table[InstructionType.Sin.ToByte()]         = (y, _) => Math.Sin(y);
            Table[InstructionType.Sinh.ToByte()]        = (y, _) => Math.Sinh(y);
            Table[InstructionType.Asin.ToByte()]        = (y, _) => Math.Asin(y);
            Table[InstructionType.Asinh.ToByte()]       = (y, _) => Math.Asinh(y);

            Table[InstructionType.Cos.ToByte()]         = (y, _) => Math.Cos(y);
            Table[InstructionType.Cosh.ToByte()]        = (y, _) => Math.Cosh(y);
            Table[InstructionType.Acos.ToByte()]        = (y, _) => Math.Acos(y);
            Table[InstructionType.Acosh.ToByte()]       = (y, _) => Math.Acosh(y);

            Table[InstructionType.Tan.ToByte()]         = (y, _) => Math.Tan(y);
            Table[InstructionType.Tanh.ToByte()]        = (y, _) => Math.Tanh(y);
            Table[InstructionType.Atan.ToByte()]        = (y, _) => Math.Atan(y);
            Table[InstructionType.Atanh.ToByte()]       = (y, _) => Math.Atanh(y);
        }

        public static Func<double, double, double>[] Table { get; } =
            new Func<double, double, double>[InstructionType.Count.ToByte()];

        public static byte ToByte(this InstructionType type)
        {
            return (byte)type;
        }

        public static int Size(this InstructionType type)
        {
            return type switch
            {
                InstructionType.LoadNumber  => Instruction.Size + Number.Size,

                InstructionType.Jmp         => Instruction.Size + CodeAddress.Size,
                InstructionType.Jz          => Instruction.Size + CodeAddress.Size,

                InstructionType.Load        => Instruction.Size + MemoryAddress.Size,
                InstructionType.Store       => Instruction.Size + MemoryAddress.Size,

                _ => Instruction.Size,
            };
        }
    }

    public class Program
    {
        #region Constructors

        public Program(Expression expression, MemoryMap map)
        {
            Instructions = new(expression.Tokens.Length);

            Instructions.AddInstruction(InstructionType.Start);

            BranchStack stack = new();

            int lastExprStart = 0;

            for (int i = 0; i < expression.Tokens.Length; i++)
            {
                Token token = expression.Tokens[i];
                BaseToken current = token.Base;
                switch (current.Type)
                {
                    case TokenType.Number:
                        if (double.TryParse(current.Symbol, out double value))
                        {
                            Number number = new(value);
                            Instructions.AddInstruction(
                                InstructionType.LoadNumber,
                                number.GetBytes());
                            break;
                        }

                        throw new InterpreterException(token.Line, "Cannot parse number!");
                    case TokenType.Parameter:
                    case TokenType.Variable:
                        Instructions.AddInstruction(
                            InstructionType.Load,
                            map[current.Symbol].GetBytes());
                        break;
                    case TokenType.Input:
                        Instructions.AddInstruction(InstructionType.LoadIn);
                        break;
                    case TokenType.Output:
                        Instructions.AddInstruction(InstructionType.LoadOut);
                        break;
                    case TokenType.Constant:
                        Instructions.AddInstruction(
                            OnConstant(token.Line, current.Symbol));
                        break;
                    case TokenType.Branch:
                        BranchContext context = new(i, new(lastExprStart), new(Instructions.Count));
                        stack.Push(context);
                        break;
                    case TokenType.BranchEnd:
                        if (stack.TryPop(out BranchContext ctx))
                        {
                            Token oldtoken = expression.Tokens[ctx.Index];
                            if (oldtoken.Base.Symbol == Tokens.BRANCH_WHILE)
                            {
                                Instructions.AddInstruction(
                                    InstructionType.Jmp,
                                    ctx.Condition.GetBytes());
                            }

                            // Not - 1 because the insert readjusts it again
                            CodeAddress address = new(Instructions.Count);

                            Instructions.InsertInstruction(ctx.Insert,
                                InstructionType.Jz,
                                address.GetBytes());
                            break;
                        }

                        throw new InterpreterException(token.Line, "Unexpected branch end!");
                    case TokenType.Assignment:
                        InstructionType type = OnAssignment(token.Line, current.Symbol);

                        // MUTATES i, because we don't want to add this token again on the next iteration
                        Token target = expression.Tokens[++i];
                        string symbol = target.Base.Symbol;

                        if (symbol == Tokens.INPUT)
                        {
                            OnAssignment(InstructionType.LoadIn, type, InstructionType.StoreIn,
                                Array.Empty<byte>());
                        }
                        else if (symbol == Tokens.OUTPUT)
                        {
                            OnAssignment(InstructionType.LoadOut, type, InstructionType.StoreOut,
                                Array.Empty<byte>());
                        }
                        else
                        {
                            MemoryAddress address = map[symbol];
                            byte[] pointer = address.GetBytes();
                            OnAssignment(InstructionType.Load, type, InstructionType.Store, pointer);
                        }

                        lastExprStart = i; // Takes into account the program counter incrementing
                        break;
                    case TokenType.Arithmetic:
                        OnArithmetic(token.Line, current.Symbol);
                        break;
                    case TokenType.Comparison:
                        Instructions.AddInstruction(OnComparison(token.Line, current.Symbol));
                        break;
                    case TokenType.Function:
                        Instructions.AddInstruction(OnFunction(token.Line, current.Symbol));
                        break;
                    default:
                        throw new InterpreterException(token.Line, "Cannot emit token!");
                }
            }

            Instructions.AddInstruction(InstructionType.End);

            Instructions.TrimExcess();
        }

        #endregion Constructors

        #region Properties

        public InstructionList Instructions { get; }

        #endregion Properties

        #region Jump Tables

        private static InstructionType OnConstant(uint line, string symbol)
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

        private static InstructionType OnAssignment(uint line, string symbol)
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

        private void OnArithmetic(uint line, string symbol)
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
                        Instructions[lastIndex].GrabType() == InstructionType.LoadE)
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

        private static InstructionType OnComparison(uint line, string symbol)
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

        private static InstructionType OnFunction(uint line, string symbol)
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
            Add(new(type));
        }

        public void AddInstruction(InstructionType type, byte[] data)
        {
            Instruction instruction = new(type);
            instruction.CopyFrom(data);
            Add(instruction);
        }

        public void InsertInstruction(CodeAddress address, InstructionType type, byte[] data)
        {
            Instruction instruction = new(type);
            instruction.CopyFrom(data);
            Insert(address.Address, instruction);
        }
    }

    public class MemoryHeap
    {
        private readonly double[] Mem = new double[Parsing.CAPACITY];

        public double this[MemoryAddress address]
        {
            get { return Mem[address.Address]; }
            set { Mem[address.Address] = value; }
        }

        public void RestoreTo(MemoryHeap other)
        {
            other.Mem.CopyTo(Mem, 0);
        }
    }

    public class ParameterPairs : IEnumerable<ParameterPairs.ParameterNameValue?>
    {
        public readonly record struct ParameterNameValue(string Name, double Value);

        private readonly ParameterNameValue?[] Parameters =
            new ParameterNameValue?[Parsing.MAX_PARAMETERS];

        /// <summary>
        /// Accesses the parameter at an index.
        /// </summary>
        /// <param name="index">The index to get/set an element.</param>
        /// <returns>The element at this index, null if there is not an element.</returns>
        public ParameterNameValue? this[int index]
        {
            get { return Parameters[index]; }
            set { Parameters[index] = value; }
        }

        public IEnumerator<ParameterNameValue?> GetEnumerator()
        {
            return ((IEnumerable<ParameterNameValue?>)Parameters).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Parameters.GetEnumerator();
        }
    }

    public class MemoryMap : Dictionary<string, MemoryAddress>, IDictionary<string, MemoryAddress>
    { }

    public class ProgramStack : Stack<Number>
    { }

    public class BranchStack : Stack<BranchContext>
    { }
}
