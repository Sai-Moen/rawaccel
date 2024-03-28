using scripting.Common;
using scripting.Script;
using scripting.Syntactical;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace scripting.Interpretation;

/// <summary>
/// Executes Programs.
/// </summary>
public class Interpreter : IInterpreter
{
    #region Fields

    private Number x = Number.DEFAULT_X;
    private Number y = Number.DEFAULT_Y;

    private readonly Dictionary<string, MemoryAddress> addresses = [];

    private readonly MemoryHeap stable;
    private readonly MemoryHeap unstable;
    private readonly Stack<Number> mainStack = new();

    private readonly Program mainProgram;
    private readonly Program[] startup;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes the script and its default settings.
    /// </summary>
    /// <param name="syntactic">result of syntactic analysis</param>
    /// <exception cref="InterpreterException"/>
    public Interpreter(ParsingResult syntactic)
    {
        Description = syntactic.Description;

        Parameters parameters = syntactic.Parameters;
        int numParameters = parameters.Count;

        Debug.Assert(numParameters <= Constants.MAX_PARAMETERS);
        for (int i = 0; i < numParameters; i++)
        {
            MemoryAddress address = i;
            addresses.Add(parameters[i].Name, address);
        }

        Variables variables = syntactic.Variables;
        int numVariables = variables.Count;
        startup = new Program[numVariables];

        int capacity = Constants.MAX_PARAMETERS + numVariables;
        stable = new(capacity);
        unstable = new(capacity);

        Debug.Assert(numVariables <= Constants.MAX_VARIABLES);
        for (int i = 0; i < numVariables; i++)
        {
            MemoryAddress address = Constants.MAX_PARAMETERS + i;
            Variable variable = variables[i];
            addresses.Add(variable.Name, address);
            startup[i] = new(variable.Expr, addresses);
        }

        mainProgram = new(syntactic.Tokens, addresses);

        // responsibility to change settings from script defaults to saved settings is on the caller
        Defaults = new(parameters);
        Settings = parameters.Clone();
    }

    #endregion

    #region Properties

    public string Description { get; }

    public ReadOnlyParameters Defaults { get; }
    public Parameters Settings { get; }

    #endregion

    #region Methods

    public void Init()
    {
        foreach (Parameter parameter in Settings)
        {
            // most cache-friendly operation
            stable[addresses[parameter.Name]] = parameter.Value;
        }
        unstable.CopyFrom(stable);

        Stack<Number> stack = new();
        foreach (Program program in startup)
        {
            ExecuteProgram(program, stack);
        }
        stable.CopyFrom(unstable);
    }

    public double Calculate(double x)
    {
        this.x = x;
        ExecuteProgram(mainProgram, mainStack);
        double y = this.y;

        unstable.CopyFrom(stable);
        this.y = Number.DEFAULT_Y;
        return y;
    }

    private void ExecuteProgram(Program program, Stack<Number> stack)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Fn1(Func<Number, Number> func)
        {
            Debug.Assert(stack.Count >= 1);
            stack.Push(func(stack.Pop()));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Fn2(Func<Number, Number, Number> func)
        {
            Debug.Assert(stack.Count >= 2);
            stack.Push(func(stack.Pop(), stack.Pop()));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Fn3(Func<Number, Number, Number, Number> func)
        {
            Debug.Assert(stack.Count >= 3);
            stack.Push(func(stack.Pop(), stack.Pop(), stack.Pop()));
        }

        for (CodeAddress c = 0; c < program.Length; c++)
        switch (program[c].instruction.Type)
        {
            case InstructionType.Start:
                break;
            case InstructionType.End:
                if (c != program.Length - 1)
                {
                    InterpreterError("Unexpected program end!");
                }

                Debug.Assert(stack.Count == 0);
                return;
            case InstructionType.Return:
                stack.Clear(); // caller may expect empty stack back
                return;
            case InstructionType.Load:
                MemoryAddress loadAddress = (MemoryAddress)program.GetOperandFromNext(ref c);
                stack.Push(unstable[loadAddress]);
                break;
            case InstructionType.Store:
                MemoryAddress storeAddress = (MemoryAddress)program.GetOperandFromNext(ref c);
                unstable[storeAddress] = stack.Pop();
                break;
            case InstructionType.LoadIn:
                stack.Push(x);
                break;
            case InstructionType.StoreIn:
                x = stack.Pop();
                break;
            case InstructionType.LoadOut:
                stack.Push(y);
                break;
            case InstructionType.StoreOut:
                y = stack.Pop();
                break;
            case InstructionType.LoadNumber:
                DataAddress dAddress = (DataAddress)program.GetOperandFromNext(ref c);
                stack.Push(program[dAddress]);
                break;
            case InstructionType.Swap:
                Number swap1 = stack.Pop();
                Number swap2 = stack.Pop();
                stack.Push(swap1);
                stack.Push(swap2);
                break;
            case InstructionType.Jmp:
                CodeAddress jmpAddress = (CodeAddress)program.GetOperandFromNext(ref c);
                c = jmpAddress;
                break;
            case InstructionType.Jz:
                CodeAddress jzAddress = (CodeAddress)program.GetOperandFromNext(ref c);
                if (!stack.Pop())
                {
                    c = jzAddress;
                }
                break;
            case InstructionType.LoadE:
                stack.Push(Math.E);
                break;
            case InstructionType.LoadPi:
                stack.Push(Math.PI);
                break;
            case InstructionType.LoadTau:
                stack.Push(Math.Tau);
                break;
            case InstructionType.LoadZero:
                stack.Push(Number.ZERO);
                break;
            case InstructionType.Add:
                Fn2((y, x) => x + y);
                break;
            case InstructionType.Sub:
                Fn2((y, x) => x - y);
                break;
            case InstructionType.Mul:
                Fn2((y, x) => x * y);
                break;
            case InstructionType.Div:
                Fn2((y, x) => x / y);
                break;
            case InstructionType.Mod:
                Fn2((y, x) => x % y);
                break;
            case InstructionType.Pow:
                Fn2((y, x) => Math.Pow(x, y));
                break;
            case InstructionType.Exp: // implicit first argument
                Fn1(a => Math.Exp(a));
                break;
            case InstructionType.Or:
                Fn2((y, x) => x | y);
                break;
            case InstructionType.And:
                Fn2((y, x) => x & y);
                break;
            case InstructionType.Lt:
                Fn2((y, x) => x < y);
                break;
            case InstructionType.Gt:
                Fn2((y, x) => x > y);
                break;
            case InstructionType.Le:
                Fn2((y, x) => x <= y);
                break;
            case InstructionType.Ge:
                Fn2((y, x) => x >= y);
                break;
            case InstructionType.Eq:
                Fn2((y, x) => x == y);
                break;
            case InstructionType.Ne:
                Fn2((y, x) => x != y);
                break;
            case InstructionType.Not: // unary
                stack.Push(!stack.Pop());
                break;
            case InstructionType.Abs:
                Fn1(a => Math.Abs(a));
                break;
            case InstructionType.Sign:
                Fn1(a => Math.Sign(a));
                break;
            case InstructionType.CopySign:
                Fn2((b, a) => Math.CopySign(a, b));
                break;
            case InstructionType.Round:
                Fn1(a => Math.Round(a));
                break;
            case InstructionType.Trunc:
                Fn1(a => Math.Truncate(a));
                break;
            case InstructionType.Ceil:
                Fn1(a => Math.Ceiling(a));
                break;
            case InstructionType.Floor:
                Fn1(a => Math.Floor(a));
                break;
            case InstructionType.Clamp:
                Fn3((c, b, a) => Math.Clamp(a, b, c));
                break;
            case InstructionType.Min:
                Fn2((b, a) => Math.Min(a, b));
                break;
            case InstructionType.Max:
                Fn2((b, a) => Math.Max(a, b));
                break;
            case InstructionType.MinM:
                Fn2((b, a) => Math.MinMagnitude(a, b));
                break;
            case InstructionType.MaxM:
                Fn2((b, a) => Math.MaxMagnitude(a, b));
                break;
            case InstructionType.Sqrt:
                Fn1(a => Math.Sqrt(a));
                break;
            case InstructionType.Cbrt:
                Fn1(a => Math.Cbrt(a));
                break;
            case InstructionType.Log:
                Fn1(a => Math.Log(a));
                break;
            case InstructionType.Log2:
                Fn1(a => Math.Log2(a));
                break;
            case InstructionType.Log10:
                Fn1(a => Math.Log10(a));
                break;
            case InstructionType.LogN:
                Fn2((b, a) => Math.Log(a, b));
                break;
            case InstructionType.Sin:
                Fn1(a => Math.Sin(a));
                break;
            case InstructionType.Sinh:
                Fn1(a => Math.Sinh(a));
                break;
            case InstructionType.Asin:
                Fn1(a => Math.Asin(a));
                break;
            case InstructionType.Asinh:
                Fn1(a => Math.Asinh(a));
                break;
            case InstructionType.Cos:
                Fn1(a => Math.Cos(a));
                break;
            case InstructionType.Cosh:
                Fn1(a => Math.Cosh(a));
                break;
            case InstructionType.Acos:
                Fn1(a => Math.Acos(a));
                break;
            case InstructionType.Acosh:
                Fn1(a => Math.Acosh(a));
                break;
            case InstructionType.Tan:
                Fn1(a => Math.Tan(a));
                break;
            case InstructionType.Tanh:
                Fn1(a => Math.Tanh(a));
                break;
            case InstructionType.Atan:
                Fn1(a => Math.Atan(a));
                break;
            case InstructionType.Atanh:
                Fn1(a => Math.Atanh(a));
                break;
            case InstructionType.Atan2:
                Fn2((b, a) => Math.Atan2(a, b));
                break;
            case InstructionType.FusedMultiplyAdd:
                Fn3((c, b, a) => Math.FusedMultiplyAdd(a, b, c));
                break;
            case InstructionType.ScaleB:
                Fn2((b, a) => Math.ScaleB(a, (int)b)); // lol
                break;
            default:
                InterpreterError("Not an instruction!");
                break;
        }
    }

#endregion

    private static void InterpreterError(string error)
    {
        throw new InterpreterException(error);
    }
}
