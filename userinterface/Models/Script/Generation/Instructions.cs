using System;
using System.Collections.Generic;

namespace userinterface.Models.Script.Generation
{
    public enum InstructionType : byte
    {
        Noop,   // Undefined state, uninitialized instruction.

        Push, Pop,          // Adds to or Takes from the Top Of Stack (TOS).
        Load, Store,        // Gets or Sets an Address in 'virtual' heap, to/from TOS.
        LoadIn, StoreIn,    // Gets or Sets the input register (x), to/from TOS.
        LoadOut, StoreOut,  // Gets or Sets the output register (y), to/from TOS.

        LoadE, LoadPi, LoadTau, LoadZero,   // Loads a constant to TOS

        // Branch,
        // Evaluates the TOS and jumps/skips to the next unconditional instruction if zero (Jz).
        // The jump itself can be unconditional (Jmp) instead, to implement loops.
        Jmp, Jz,

        // Operator,
        // does an operation on the second and first Stack item respectively,
        // pushes the result onto the stack if the next instruction is not another operator.
        Add, Sub, Mul, Div, Mod, Exp,

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
    }

    public record struct InstructionAddress(byte Address);

    public class Instruction
    {
        public Instruction(InstructionType type)
        {
            ByteCode = new byte[type.Size()];
            ByteCode[0] = type.ToByte();
        }

        public byte[] ByteCode { get; }

    }

    public static class Instructions
    {
        public static Instruction[] Emit(Expression tokens)
        {
            return new Instruction[0];
        }

        public static byte ToByte(this InstructionType type)
        {
            return Convert.ToByte(type);
        }

        public static int Size(this InstructionType type) =>
            type switch
            {
                InstructionType.Load => 2,
                InstructionType.Store => 2,

                InstructionType.Jmp => 3,
                InstructionType.Jz => 3,

                _ => 1,
        };
    }

    public class InterpreterHeap
    {
        public InterpreterHeap(
            ParameterAssignment[] parameters,
            VariableAssignment[] variables)
        {
            Slots.CopyTo(parameters, 0);
            Slots.CopyTo(variables, Tokens.MAX_PARAMETERS);
        }

        public InterpreterHeap(
            List<ParameterAssignment> parameters,
            List<VariableAssignment> variables)
            : this(parameters.ToArray(), variables.ToArray())
        {
        }

        public double[] Memory { get; } = new double[Tokens.CAPACITY];

        public ParserNode[] Slots { get; } = new ParserNode[Tokens.CAPACITY];
    }
}
