using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace userinterface.Models.Script.Generation
{
    public enum InstructionType : byte
    {
        End,   // End of script marker

        Push, Pop,          // Adds to or Takes from the Top Of Stack (TOS).
        Load, Store,        // Gets or Sets an Address in 'virtual' heap, to/from TOS.
        LoadIn, StoreIn,    // Gets or Sets the input register (x), to/from TOS.
        LoadOut, StoreOut,  // Gets or Sets the output register (y), to/from TOS.
        LoadNumber,         // Loads a number

        LoadE, LoadPi, LoadTau, LoadZero,   // Loads a constant to TOS

        // Branch,
        // Evaluates the TOS and jumps/skips to the next branch end marker if zero (Jz).
        // The jump itself can be unconditional (Jmp) instead, to implement loops (Jmp backwards).
        Jmp, Jz,

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

        // Leave this at the bottom of the enum for obvious reasons
        Count,
    }

    public readonly struct Instruction
    {
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

        public void CopyFrom(byte[] bytes)
        {
            bytes.CopyTo(ByteCode, 1);
        }
    }

    public struct MemoryAddress
    {
        public const byte MaxValue = byte.MaxValue;

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

        public byte Address { get; }

        public readonly byte[] GetBytes()
        {
            return new byte[1]{ Address };
        }
    }

    public struct CodeAddress
    {
        public const ushort MaxValue = ushort.MaxValue;

        public CodeAddress(int address)
        {
            if (address > MaxValue)
            {
                throw new InterpreterException("Code address overflow!");
            }

            Address = (ushort)address;
        }

        public ushort Address { get; }

        public readonly byte[] GetBytes()
        {
            return BitConverter.GetBytes(Address);
        }
    }

    public static class Instructions
    {
        static Instructions()
        {
            // Arguments are reversed because of the Stack Pop order

            Table[InstructionType.LoadE.ToByte()] = (_, _) => Math.E;
            Table[InstructionType.LoadPi.ToByte()] = (_, _) => Math.PI;
            Table[InstructionType.LoadTau.ToByte()] = (_, _) => Math.Tau;
            Table[InstructionType.LoadZero.ToByte()] = (_, _) => 0;

            Table[InstructionType.Add.ToByte()] = (y, x) => x + y;
            Table[InstructionType.Sub.ToByte()] = (y, x) => x - y;
            Table[InstructionType.Mul.ToByte()] = (y, x) => x * y;
            Table[InstructionType.Div.ToByte()] = (y, x) => x / y;
            Table[InstructionType.Mod.ToByte()] = (y, x) => x % y;
            Table[InstructionType.Exp.ToByte()] = (y, x) => Math.Pow(x, y);
            Table[InstructionType.ExpE.ToByte()] = (y, _) => Math.Exp(y);

            Table[InstructionType.Or.ToByte()] = (y, x) => Convert.ToDouble((x != 0) | (y != 0));
            Table[InstructionType.And.ToByte()] = (y, x) => Convert.ToDouble((x != 0) & (y != 0));
            Table[InstructionType.Lt.ToByte()] = (y, x) => Convert.ToDouble(x < y);
            Table[InstructionType.Gt.ToByte()] = (y, x) => Convert.ToDouble(x > y);
            Table[InstructionType.Le.ToByte()] = (y, x) => Convert.ToDouble(x <= y);
            Table[InstructionType.Ge.ToByte()] = (y, x) => Convert.ToDouble(x >= y);
            Table[InstructionType.Eq.ToByte()] = (y, x) => Convert.ToDouble(x == y);
            Table[InstructionType.Ne.ToByte()] = (y, x) => Convert.ToDouble(x != y);
            Table[InstructionType.Not.ToByte()] = (y, _) => Convert.ToDouble(y == 0);

            Table[InstructionType.Abs.ToByte()] = (y, _) => Math.Abs(y);
            Table[InstructionType.Sqrt.ToByte()] = (y, _) => Math.Sqrt(y);
            Table[InstructionType.Cbrt.ToByte()] = (y, _) => Math.Cbrt(y);

            Table[InstructionType.Round.ToByte()] = (y, _) => Math.Round(y);
            Table[InstructionType.Trunc.ToByte()] = (y, _) => Math.Truncate(y);
            Table[InstructionType.Ceil.ToByte()] = (y, _) => Math.Ceiling(y);
            Table[InstructionType.Floor.ToByte()] = (y, _) => Math.Floor(y);

            Table[InstructionType.Log.ToByte()] = (y, _) => Math.Log(y);
            Table[InstructionType.Log2.ToByte()] = (y, _) => Math.Log2(y);
            Table[InstructionType.Log10.ToByte()] = (y, _) => Math.Log10(y);

            Table[InstructionType.Sin.ToByte()] = (y, _) => Math.Sin(y);
            Table[InstructionType.Sinh.ToByte()] = (y, _) => Math.Sinh(y);
            Table[InstructionType.Asin.ToByte()] = (y, _) => Math.Asin(y);
            Table[InstructionType.Asinh.ToByte()] = (y, _) => Math.Asinh(y);

            Table[InstructionType.Cos.ToByte()] = (y, _) => Math.Cos(y);
            Table[InstructionType.Cosh.ToByte()] = (y, _) => Math.Cosh(y);
            Table[InstructionType.Acos.ToByte()] = (y, _) => Math.Acos(y);
            Table[InstructionType.Acosh.ToByte()] = (y, _) => Math.Acosh(y);

            Table[InstructionType.Tan.ToByte()] = (y, _) => Math.Tan(y);
            Table[InstructionType.Tanh.ToByte()] = (y, _) => Math.Tanh(y);
            Table[InstructionType.Atan.ToByte()] = (y, _) => Math.Atan(y);
            Table[InstructionType.Atanh.ToByte()] = (y, _) => Math.Atanh(y);
        }

        public static Func<double, double, double>[] Table { get; } =
            new Func<double, double, double>[InstructionType.Count.ToByte()];

        public static byte ToByte(this InstructionType type) => (byte)type;

        public static int Size(this InstructionType type)
        {
            return type switch
            {
                InstructionType.LoadNumber => 9,

                InstructionType.Jmp => 3,
                InstructionType.Jz => 3,

                InstructionType.Load => 2,
                InstructionType.Store => 2,

                _ => 1,
            };
        }
    }

    public class Program
    {
        private readonly InstructionList _instructions;

        #region Constructors

        public Program(Expression expression, MemoryMap map)
        {
            _instructions = new(expression.Tokens.Length);

            BranchCallback callback = new();

            for (int i = 0; i < expression.Tokens.Length; i++)
            {
                Token token = expression.Tokens[i];
                BaseToken current = token.Base;
                switch (current.Type)
                {
                    case TokenType.Number:
                        if (double.TryParse(current.Symbol, out double value))
                        {
                            _instructions.AddInstruction(
                                InstructionType.LoadNumber,
                                BitConverter.GetBytes(value));
                            break;
                        }

                        throw new InterpreterException(token.Line, "Cannot parse number!");
                    case TokenType.Parameter:
                    case TokenType.Variable:
                        if (map.TryGetValue(current.Symbol, out MemoryAddress loadIndex))
                        {
                            _instructions.AddInstruction(
                                InstructionType.Load,
                                loadIndex.GetBytes());
                            break;
                        }

                        throw new InterpreterException(token.Line, "Unable to obtain address!");
                    case TokenType.Input:
                        _instructions.AddInstruction(InstructionType.LoadIn);
                        break;
                    case TokenType.Output:
                        _instructions.AddInstruction(InstructionType.LoadOut);
                        break;
                    case TokenType.Constant:
                        _instructions.AddInstruction(
                            OnConstant(token.Line, current.Symbol));
                        break;
                    case TokenType.Branch:
                        callback.Push(() => (i, new(_instructions.Count)));
                        break;
                    case TokenType.BranchEnd:
                        if (callback.TryPop(out var result))
                        {
                            (int old, CodeAddress insert) = result();

                            Token oldtoken = expression.Tokens[old];
                            if (oldtoken.Base.Symbol == Tokens.BRANCH_WHILE)
                            {
                                _instructions.AddInstruction(
                                    InstructionType.Jmp,
                                    insert.GetBytes());
                            }

                            // + 1, since the insert will realign it with the first unconditional instruction
                            CodeAddress address = new(_instructions.Count + 1);

                            _instructions.InsertInstruction(
                                insert.Address,
                                InstructionType.Jz,
                                address.GetBytes());
                            break;
                        }

                        throw new InterpreterException(token.Line, "Unexpected branch end!");
                    case TokenType.Assignment:
                        // MUTATES i, because we don't want to add it again on the next iteration
                        Token target = expression.Tokens[++i];
                        string symbol = target.Base.Symbol;

                        if (symbol == Tokens.INPUT)
                        {
                            _instructions.AddInstruction(InstructionType.StoreIn);
                            break;
                        }
                        else if (symbol == Tokens.OUTPUT)
                        {
                            _instructions.AddInstruction(InstructionType.StoreOut);
                            break;
                        }
                        else if (map.TryGetValue(symbol, out MemoryAddress storeIndex))
                        {
                            OnAssignment(token.Line, current.Symbol, storeIndex.GetBytes());
                            break;
                        }

                        throw new InterpreterException(token.Line, "Unable to obtain target address!");
                    case TokenType.Arithmetic:
                        OnArithmetic(token.Line, current.Symbol);
                        break;
                    case TokenType.Comparison:
                        _instructions.AddInstruction(OnComparison(token.Line, current.Symbol));
                        break;
                    case TokenType.Function:
                        _instructions.AddInstruction(OnFunction(token.Line, current.Symbol));
                        break;
                    default:
                        throw new InterpreterException(token.Line, "Cannot emit token!");
                }
            }

            _instructions.AddInstruction(InstructionType.End);

            Instructions = _instructions.ToArray();
        }

        #endregion Constructors

        #region Properties

        public Instruction[] Instructions { get; }

        #endregion Properties

        #region Jump Tables

        private InstructionType OnConstant(uint line, string symbol)
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

        private void OnAssignment(uint line, string symbol, byte[] ptr)
        {
            void LoadModifyStore(InstructionType modification)
            {
                _instructions.AddInstruction(InstructionType.Load, ptr);
                _instructions.AddInstruction(modification);
                _instructions.AddInstruction(InstructionType.Store, ptr);
            }

            switch (symbol)
            {
                case Tokens.ASSIGN:
                    _instructions.AddInstruction(InstructionType.Store, ptr);
                    break;
                case Tokens.IADD:
                    LoadModifyStore(InstructionType.Add);
                    break;
                case Tokens.ISUB:
                    LoadModifyStore(InstructionType.Sub);
                    break;
                case Tokens.IMUL:
                    LoadModifyStore(InstructionType.Mul);
                    break;
                case Tokens.IDIV:
                    LoadModifyStore(InstructionType.Div);
                    break;
                case Tokens.IMOD:
                    LoadModifyStore(InstructionType.Mod);
                    break;
                case Tokens.IEXP:
                    LoadModifyStore(InstructionType.Exp);
                    break;
                default:
                    throw new InterpreterException(line, "Cannot emit assignment!");
            }
        }

        private void OnArithmetic(uint line, string symbol)
        {
            switch (symbol)
            {
                case Tokens.ADD:
                    _instructions.AddInstruction(InstructionType.Add);
                    break;
                case Tokens.SUB:
                    _instructions.AddInstruction(InstructionType.Sub);
                    break;
                case Tokens.MUL:
                    _instructions.AddInstruction(InstructionType.Mul);
                    break;
                case Tokens.DIV:
                    _instructions.AddInstruction(InstructionType.Div);
                    break;
                case Tokens.MOD:
                    _instructions.AddInstruction(InstructionType.Mod);
                    break;
                case Tokens.EXP:
                    // Try to convert E^ -> Exp()
                    if (_instructions.Count > 0 &&
                        _instructions[^1].GrabType() == InstructionType.LoadE)
                    {
                        _instructions.RemoveAt(_instructions.Count - 1);
                        _instructions.AddInstruction(InstructionType.ExpE);
                    }
                    else
                    {
                        _instructions.AddInstruction(InstructionType.Exp);
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

        public void InsertInstruction(int index, InstructionType type, byte[] data)
        {
            Instruction instruction = new(type);
            instruction.CopyFrom(data);
            Insert(index, instruction);
        }
    }

    public class MemoryHeap
    {
        private readonly double[] Mem = new double[Tokens.CAPACITY];

        public double this[int index]
        {
            get { return Mem[index]; }
            set { Mem[index] = value; }
        }

        public void RestoreTo(MemoryHeap other)
        {
            other.Mem.CopyTo(Mem, 0);
        }
    }

    public class ParameterNameValuePairs
    {
        public readonly record struct ParameterNameValue(string Name, double Value);

        public ParameterNameValue[] Parameters { get; set; } =
            new ParameterNameValue[Tokens.MAX_PARAMETERS];

        public ParameterNameValue this[int index]
        {
            get { return Parameters[index]; }
            set { Parameters[index] = value; }
        }
    }

    public class MemoryMap : Dictionary<string, MemoryAddress>, IDictionary<string, MemoryAddress>
    { }

    public class BranchCallback : Stack<Func<(int, CodeAddress)>>
    { }
}
