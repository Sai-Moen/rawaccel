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

        public static Instruction[] Emit(Expression expression, IDictionary<string, MemoryAddress> indices)
        {
            InstructionList instructions = new(expression.Tokens.Length << 1);

            Stack<Func<(int, CodeAddress)>> callback = new();

            for (int i = 0; i < expression.Tokens.Length; i++)
            {
                Token token = expression.Tokens[i];
                BaseToken current = token.Base;
                switch (current.Type)
                {
                    case TokenType.Number:
                        if (double.TryParse(current.Symbol, out double value))
                        {
                            instructions.AddInstruction(
                                InstructionType.LoadNumber,
                                BitConverter.GetBytes(value));
                            break;
                        }

                        throw new InterpreterException(token.Line, "Cannot parse number!");
                    case TokenType.Parameter:
                    case TokenType.Variable:
                        if (indices.TryGetValue(current.Symbol, out MemoryAddress loadIndex))
                        {
                            instructions.AddInstruction(
                                InstructionType.Load,
                                loadIndex.GetBytes());
                            break;
                        }

                        throw new InterpreterException(token.Line, "Unable to obtain address!");
                    case TokenType.Input:
                        instructions.AddInstruction(InstructionType.LoadIn);
                        break;
                    case TokenType.Output:
                        instructions.AddInstruction(InstructionType.LoadOut);
                        break;
                    case TokenType.Constant:
                        OnConstant(instructions, token, current);
                        break;
                    case TokenType.Branch:
                        callback.Push(() => (i, new(instructions.Count)));
                        break;
                    case TokenType.BranchEnd:
                        if (callback.TryPop(out var result))
                        {
                            (int old, CodeAddress insert) = result();

                            Token oldtoken = expression.Tokens[old];
                            if (oldtoken.Base.Symbol == Tokens.BRANCH_WHILE)
                            {
                                instructions.AddInstruction(
                                    InstructionType.Jmp,
                                    insert.GetBytes());
                            }

                            // + 1, since the insert will realign it with the first unconditional instruction
                            CodeAddress address = new(instructions.Count + 1);

                            instructions.InsertInstruction(
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
                            instructions.AddInstruction(InstructionType.StoreIn);
                            break;
                        }
                        else if (symbol == Tokens.OUTPUT)
                        {
                            instructions.AddInstruction(InstructionType.StoreOut);
                            break;
                        }
                        else if (indices.TryGetValue(symbol, out MemoryAddress storeIndex))
                        {
                            OnAssignment(instructions, token, current, storeIndex.GetBytes());
                            break;
                        }

                        throw new InterpreterException(token.Line, "Unable to obtain target address!");
                    case TokenType.Arithmetic:
                        OnArithmetic(instructions, token, current);
                        break;
                    case TokenType.Comparison:
                        OnComparison(instructions, token, current);
                        break;
                    case TokenType.Function:
                        OnFunction(instructions, token, current);
                        break;
                    default:
                        throw new InterpreterException(token.Line, "Cannot emit token!");
                }
            }

            instructions.AddInstruction(InstructionType.End);

            return instructions.ToArray();
        }

        private static void OnConstant(
            in InstructionList instructions,
            Token token, BaseToken current)
        {
            switch (current.Symbol)
            {
                case Tokens.CONST_E:
                    instructions.AddInstruction(InstructionType.LoadE);
                    break;
                case Tokens.CONST_PI:
                    instructions.AddInstruction(InstructionType.LoadPi);
                    break;
                case Tokens.CONST_TAU:
                    instructions.AddInstruction(InstructionType.LoadTau);
                    break;
                case Tokens.ZERO:
                    instructions.AddInstruction(InstructionType.LoadZero);
                    break;
                default:
                    throw new InterpreterException(token.Line, "Cannot emit constant!");
            }
        }

        private static void OnAssignment(
            in InstructionList instructions,
            Token token, BaseToken current,
            byte[] ptr)
        {
            void LoadModifyStore(in InstructionList instructions, InstructionType modification)
            {
                instructions.AddInstruction(InstructionType.Load, ptr);
                instructions.AddInstruction(modification);
                instructions.AddInstruction(InstructionType.Store, ptr);
            }

            switch (current.Symbol)
            {
                case Tokens.ASSIGN:
                    instructions.AddInstruction(InstructionType.Store, ptr);
                    break;
                case Tokens.IADD:
                    LoadModifyStore(instructions, InstructionType.Add);
                    break;
                case Tokens.ISUB:
                    LoadModifyStore(instructions, InstructionType.Sub);
                    break;
                case Tokens.IMUL:
                    LoadModifyStore(instructions, InstructionType.Mul);
                    break;
                case Tokens.IDIV:
                    LoadModifyStore(instructions, InstructionType.Div);
                    break;
                case Tokens.IMOD:
                    LoadModifyStore(instructions, InstructionType.Mod);
                    break;
                case Tokens.IEXP:
                    LoadModifyStore(instructions, InstructionType.Exp);
                    break;
                default:
                    throw new InterpreterException(token.Line, "Cannot emit assignment!");
            }
        }

        private static void OnArithmetic(
            in InstructionList instructions,
            Token token, BaseToken current)
        {
            switch (current.Symbol)
            {
                case Tokens.ADD:
                    instructions.AddInstruction(InstructionType.Add);
                    break;
                case Tokens.SUB:
                    instructions.AddInstruction(InstructionType.Sub);
                    break;
                case Tokens.MUL:
                    instructions.AddInstruction(InstructionType.Mul);
                    break;
                case Tokens.DIV:
                    instructions.AddInstruction(InstructionType.Div);
                    break;
                case Tokens.MOD:
                    instructions.AddInstruction(InstructionType.Mod);
                    break;
                case Tokens.EXP:
                    // Try to convert E^ -> Exp()
                    if (instructions.Count > 0 &&
                        instructions[^1].GrabType() == InstructionType.LoadE)
                    {
                        instructions.RemoveAt(instructions.Count - 1);
                        instructions.AddInstruction(InstructionType.ExpE);
                    }
                    else
                    {
                        instructions.AddInstruction(InstructionType.Exp);
                    }

                    break;
                default:
                    throw new InterpreterException(token.Line, "Cannot emit arithmetic!");
            }
        }

        private static void OnComparison(
            in InstructionList instructions,
            Token token, BaseToken current)
        {
            switch (current.Symbol)
            {
                case Tokens.OR:
                    instructions.AddInstruction(InstructionType.Or);
                    break;
                case Tokens.AND:
                    instructions.AddInstruction(InstructionType.And);
                    break;
                case Tokens.LT:
                    instructions.AddInstruction(InstructionType.Lt);
                    break;
                case Tokens.GT:
                    instructions.AddInstruction(InstructionType.Gt);
                    break;
                case Tokens.LE:
                    instructions.AddInstruction(InstructionType.Le);
                    break;
                case Tokens.GE:
                    instructions.AddInstruction(InstructionType.Ge);
                    break;
                case Tokens.EQ:
                    instructions.AddInstruction(InstructionType.Eq);
                    break;
                case Tokens.NE:
                    instructions.AddInstruction(InstructionType.Ne);
                    break;
                case Tokens.NOT:
                    instructions.AddInstruction(InstructionType.Not);
                    break;
                default:
                    throw new InterpreterException(token.Line, "Cannot emit comparison!");
            }
        }

        private static void OnFunction(
            in InstructionList instructions,
            Token token, BaseToken current)
        {
            switch (current.Symbol)
            {
                case Tokens.ABS:
                    instructions.AddInstruction(InstructionType.Abs);
                    break;
                case Tokens.SQRT:
                    instructions.AddInstruction(InstructionType.Sqrt);
                    break;
                case Tokens.CBRT:
                    instructions.AddInstruction(InstructionType.Cbrt);
                    break;
                case Tokens.ROUND:
                    instructions.AddInstruction(InstructionType.Round);
                    break;
                case Tokens.TRUNC:
                    instructions.AddInstruction(InstructionType.Trunc);
                    break;
                case Tokens.CEIL:
                    instructions.AddInstruction(InstructionType.Ceil);
                    break;
                case Tokens.FLOOR:
                    instructions.AddInstruction(InstructionType.Floor);
                    break;
                case Tokens.LOG:
                    instructions.AddInstruction(InstructionType.Log);
                    break;
                case Tokens.LOG2:
                    instructions.AddInstruction(InstructionType.Log2);
                    break;
                case Tokens.LOG10:
                    instructions.AddInstruction(InstructionType.Log10);
                    break;
                case Tokens.SIN:
                    instructions.AddInstruction(InstructionType.Sin);
                    break;
                case Tokens.SINH:
                    instructions.AddInstruction(InstructionType.Sinh);
                    break;
                case Tokens.ASIN:
                    instructions.AddInstruction(InstructionType.Asin);
                    break;
                case Tokens.ASINH:
                    instructions.AddInstruction(InstructionType.Asinh);
                    break;
                case Tokens.COS:
                    instructions.AddInstruction(InstructionType.Cos);
                    break;
                case Tokens.COSH:
                    instructions.AddInstruction(InstructionType.Cosh);
                    break;
                case Tokens.ACOS:
                    instructions.AddInstruction(InstructionType.Acos);
                    break;
                case Tokens.ACOSH:
                    instructions.AddInstruction(InstructionType.Acosh);
                    break;
                case Tokens.TAN:
                    instructions.AddInstruction(InstructionType.Tan);
                    break;
                case Tokens.TANH:
                    instructions.AddInstruction(InstructionType.Tanh);
                    break;
                case Tokens.ATAN:
                    instructions.AddInstruction(InstructionType.Atan);
                    break;
                case Tokens.ATANH:
                    instructions.AddInstruction(InstructionType.Atanh);
                    break;
                default:
                    throw new InterpreterException(token.Line, "Cannot emit function!");
            }
        }
    }

    public class HeapMemory
    {
        private readonly double[] Mem = new double[Tokens.CAPACITY];

        public double this[int index]
        {
            get { return Mem[index]; }
            set { Mem[index] = value; }
        }

        public void Restore(HeapMemory other)
        {
            other.Mem.CopyTo(Mem, 0);
        }
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
}
