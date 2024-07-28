using scripting.Script;
using scripting.Generating;
using scripting.Parsing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static System.Math;
using scripting.Lexing;

namespace scripting.Interpreting;

/// <summary>
/// Executes Programs.
/// </summary>
public class Interpreter : IInterpreter
{
    #region Fields

    private readonly MemoryMap addresses = [];

    private readonly MemoryHeap stable = new();
    private readonly MemoryHeap unstable = new();
    private readonly Stack<Number> stack = new();

    private readonly Program[] startup;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes the script and its default settings.
    /// </summary>
    /// <param name="parsed">Result of parsing</param>
    /// <exception cref="InterpreterException"/>
    public Interpreter(ParsingResult parsed)
    {
        Description = parsed.Description;

        Emitter emitter = new(addresses);
        int numPersistent = 0;
        int numImpersistent = 0;

        Parameters parameters = parsed.Parameters;
        int numParameters = parameters.Count;

        Debug.Assert(numParameters <= Constants.MAX_PARAMETERS);
        foreach (Parameter parameter in parameters)
        {
            addresses.Add(parameter.Name, (MemoryAddress)numPersistent++);
        }

        IList<ASTAssign> variables = parsed.Variables;
        int numVariables = variables.Count;
        startup = new Program[numVariables];

        Debug.Assert(numVariables <= Constants.MAX_VARIABLES);
        for (int i = 0; i < numVariables; i++)
        {
            ASTAssign variable = variables[i];
            Token identifier = variable.Identifier;
            MemoryAddress address = identifier.Type switch
                {
                    TokenType.Immutable or TokenType.Persistent => (MemoryAddress)numPersistent++,
                    TokenType.Impersistent                      => (MemoryAddress)numImpersistent++,
                
                    _ => throw InterpreterError("Identifier does not have the correct type for a variable!")
                };
            addresses.Add(identifier.Symbol, address);

            ASTNode stmnt = new(ASTTag.Assign, new() { astAssign = variable });
            startup[i] = emitter.Emit([stmnt]);
        }

        stable.EnsureSizes(numPersistent, numImpersistent);
        unstable.EnsureSizes(numPersistent, numImpersistent);

        IList<ParsedCallback> callbacks = parsed.Callbacks;
        Debug.Assert(callbacks.Count > 0);

        Callbacks = new(this, callbacks[0], emitter);
        foreach (ParsedCallback cb in callbacks)
        {
            Callbacks.Add(cb, emitter);
        }

        // responsibility to change settings from script defaults to saved settings is on the caller
        Defaults = new(parameters);
        Settings = parameters.Clone();
    }

    #endregion

    #region Properties

    public string Description { get; }

    public ReadOnlyParameters Defaults { get; }
    public Parameters Settings { get; }

    public Callbacks Callbacks { get; }

    public Number X { get; set; } = Number.DEFAULT_X;
    public Number Y { get; set; } = Number.DEFAULT_Y;

    #endregion

    #region Methods

    public void Init()
    {
        foreach (Parameter parameter in Settings)
        {
            // most cache-friendly operation
            stable.SetPersistent(addresses[parameter.Name], parameter.Value);
        }
        unstable.CopyAllFrom(stable);

        foreach (Program program in startup)
        {
            Number[] remainder = ExecuteProgram(program);
            Debug.Assert(remainder.Length == 0, "Startup stack was not empty?");
        }
        stable.CopyAllFrom(unstable);
        Y = Number.DEFAULT_Y;
    }

    public void Stabilize()
    {
        unstable.CopyFrom(stable);
    }

    public Number[] ExecuteProgram(Program program)
    {
        return ExecuteProgram(program, stack);
    }

    public Number[] ExecuteProgram(Program program, Stack<Number> stack)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Fn1(Func<Number, Number> func)
        {
            Debug.Assert(stack.Count >= 1, "Stack does not have the required 1 element!");
            stack.Push(func(stack.Pop()));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Fn2(Func<Number, Number, Number> func)
        {
            Debug.Assert(stack.Count >= 2, "Stack does not have the required 2 elements!");
            stack.Push(func(stack.Pop(), stack.Pop()));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Fn3(Func<Number, Number, Number, Number> func)
        {
            Debug.Assert(stack.Count >= 3, "Stack does not have the required 3 elements!");
            stack.Push(func(stack.Pop(), stack.Pop(), stack.Pop()));
        }

        for (CodeAddress c = 0; c < program.Length; c++)
        switch ((InstructionType)program[c])
        {
            case InstructionType.Start:
                break;
            case InstructionType.End:
                if (c != program.Length - 1)
                    throw InterpreterError("Unexpected program end!");

                goto case InstructionType.Return;
            case InstructionType.Return:
                Number[] rem = [.. stack];
                stack.Clear();
                return rem;
            case InstructionType.LoadPersistent:
                {
                    MemoryAddress loadAddress = (MemoryAddress)program.ExtractAddress(ref c);
                    stack.Push(unstable.GetPersistent(loadAddress));
                }
                break;
            case InstructionType.StorePersistent:
                {
                    MemoryAddress storeAddress = (MemoryAddress)program.ExtractAddress(ref c);
                    unstable.SetPersistent(storeAddress, stack.Pop());
                }
                break;
            case InstructionType.LoadImpersistent:
                {
                    MemoryAddress loadAddress = (MemoryAddress)program.ExtractAddress(ref c);
                    stack.Push(unstable.GetImpersistent(loadAddress));
                }
                break;
            case InstructionType.StoreImpersistent:
                {
                    MemoryAddress storeAddress = (MemoryAddress)program.ExtractAddress(ref c);
                    unstable.SetImpersistent(storeAddress, stack.Pop());
                }
                break;
                case InstructionType.LoadIn:
                stack.Push(X);
                break;
            case InstructionType.StoreIn:
                X = stack.Pop();
                break;
            case InstructionType.LoadOut:
                stack.Push(Y);
                break;
            case InstructionType.StoreOut:
                Y = stack.Pop();
                break;
            case InstructionType.LoadNumber:
                DataAddress dAddress = (DataAddress)program.ExtractAddress(ref c);
                stack.Push(program[dAddress]);
                break;
            case InstructionType.Swap:
                Number swap1 = stack.Pop();
                Number swap2 = stack.Pop();
                stack.Push(swap1);
                stack.Push(swap2);
                break;
            case InstructionType.Jmp:
                CodeAddress jmpAddress = (CodeAddress)program.ExtractAddress(ref c);
                c = jmpAddress;
                break;
            case InstructionType.Jz:
                CodeAddress jzAddress = (CodeAddress)program.ExtractAddress(ref c);
                if (!stack.Pop()) c = jzAddress;
                break;
            case InstructionType.LoadZero:
                stack.Push(Number.ZERO);
                break;
            case InstructionType.LoadE:
                stack.Push(E);
                break;
            case InstructionType.LoadPi:
                stack.Push(PI);
                break;
            case InstructionType.LoadTau:
                stack.Push(Tau);
                break;
            case InstructionType.LoadCapacity:
                stack.Push(Constants.LUT_POINTS_CAPACITY);
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
                Fn2((y, x) => Pow(x, y));
                break;
            case InstructionType.Exp: // implicit first argument
                Fn1(a => Exp(a));
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
                Fn1(a => Abs(a));
                break;
            case InstructionType.Sign:
                Fn1(a => Sign(a));
                break;
            case InstructionType.CopySign:
                Fn2((b, a) => CopySign(a, b));
                break;
            case InstructionType.Round:
                Fn1(a => Round(a));
                break;
            case InstructionType.Trunc:
                Fn1(a => Truncate(a));
                break;
            case InstructionType.Floor:
                Fn1(a => Floor(a));
                break;
            case InstructionType.Ceil:
                Fn1(a => Ceiling(a));
                break;
            case InstructionType.Clamp:
                Fn3((c, b, a) => Clamp(a, b, c));
                break;
            case InstructionType.Min:
                Fn2((b, a) => Min(a, b));
                break;
            case InstructionType.Max:
                Fn2((b, a) => Max(a, b));
                break;
            case InstructionType.MinM:
                Fn2((b, a) => MinMagnitude(a, b));
                break;
            case InstructionType.MaxM:
                Fn2((b, a) => MaxMagnitude(a, b));
                break;
            case InstructionType.Sqrt:
                Fn1(a => Sqrt(a));
                break;
            case InstructionType.Cbrt:
                Fn1(a => Cbrt(a));
                break;
            case InstructionType.Log:
                Fn1(a => Log(a));
                break;
            case InstructionType.Log2:
                Fn1(a => Log2(a));
                break;
            case InstructionType.Log10:
                Fn1(a => Log10(a));
                break;
            case InstructionType.LogB:
                Fn2((b, a) => Log(a, b));
                break;
            case InstructionType.ILogB:
                Fn1(a => ILogB(a));
                break;
            case InstructionType.Sin:
                Fn1(a => Sin(a));
                break;
            case InstructionType.Sinh:
                Fn1(a => Sinh(a));
                break;
            case InstructionType.Asin:
                Fn1(a => Asin(a));
                break;
            case InstructionType.Asinh:
                Fn1(a => Asinh(a));
                break;
            case InstructionType.Cos:
                Fn1(a => Cos(a));
                break;
            case InstructionType.Cosh:
                Fn1(a => Cosh(a));
                break;
            case InstructionType.Acos:
                Fn1(a => Acos(a));
                break;
            case InstructionType.Acosh:
                Fn1(a => Acosh(a));
                break;
            case InstructionType.Tan:
                Fn1(a => Tan(a));
                break;
            case InstructionType.Tanh:
                Fn1(a => Tanh(a));
                break;
            case InstructionType.Atan:
                Fn1(a => Atan(a));
                break;
            case InstructionType.Atanh:
                Fn1(a => Atanh(a));
                break;
            case InstructionType.Atan2:
                Fn2((b, a) => Atan2(a, b));
                break;
            case InstructionType.FusedMultiplyAdd:
                Fn3((c, b, a) => FusedMultiplyAdd(a, b, c));
                break;
            case InstructionType.ScaleB:
                Fn2((b, a) => ScaleB(a, (int)b)); // lol
                break;
            default:
                throw InterpreterError("Not an instruction!");
        }

        throw InterpreterError("Unreachable: program loop exited without returning!");
    }

    #endregion

    private static InterpreterException InterpreterError(string error)
    {
        return new InterpreterException(error);
    }
}
