using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static System.Math;

namespace userspace_backend.ScriptingLanguage.Compiler;

/// <summary>
/// Exception for interpretation-related errors.
/// </summary>
public sealed class InterpreterException(string message)
    : ScriptException(message)
{ }

/// <summary>
/// Executes Programs.
/// </summary>
public class Interpreter
{
    private readonly Program[] assignments;
    private readonly MemoryHeap stable = new();
    private readonly MemoryHeap unstable = new();

    private readonly Program[] functions;

    private StackAddress stackPointer;
    private readonly ProgramStack stack = [];

    private int depth;

    public Interpreter(AST ast, Emitter emitter)
    {
        Parameters parameters = ast.Parameters;
        Debug.Assert(parameters.Count <= Constants.MAX_PARAMETERS);
        foreach (Parameter parameter in parameters)
            emitter.AddParameter(parameter.Name);

        IList<ASTNode> declarations = ast.Declarations;
        int numDeclarations = declarations.Count;
        Debug.Assert(numDeclarations <= Constants.MAX_DECLARATIONS);

        List<Program> assignmentsList = new(numDeclarations);
        List<Program> functionsList = new(numDeclarations);
        foreach (ASTNode node in declarations)
        {
            ASTUnion union = node.Union;
            switch (node.Tag)
            {
                case ASTTag.Assign:
                    {
                        ASTAssign assignment = union.astAssign;
                        emitter.AddAssign(assignment.Identifier);
                        assignmentsList.Add(emitter.Emit([node]));
                    }
                    break;
                case ASTTag.Function:
                    {
                        ASTFunction function = union.astFunction;
                        emitter.AddFunction(function.Identifier);
                        functionsList.Add(emitter.EmitFunction(function.Args, function.Code));
                    }
                    break;
                case ASTTag.Callback:
                    break;
                default:
                    throw InterpreterError("Invalid AST node for a declaration!");
            }
        }

        assignments = [.. assignmentsList];
        functions = [.. functionsList];

        stable.EnsureSizes(emitter.PersistentCount, emitter.ImpersistentCount);
        unstable.EnsureSizes(emitter.PersistentCount, emitter.ImpersistentCount);

        // responsibility to change settings from script defaults to saved settings is on the caller
        Defaults = new(parameters);
        Settings = parameters.Clone();
    }

    public ReadOnlyParameters Defaults { get; }
    public Parameters Settings { get; }

    public Number X { get; set; } = Number.DEFAULT_X;
    public Number Y { get; set; } = Number.DEFAULT_Y;

    public void Init()
    {
        int index = 0;
        foreach (Parameter parameter in Settings)
            stable.SetPersistent((MemoryAddress)index++, parameter.Value);
        unstable.CopyAllFrom(stable);

        foreach (Program program in assignments)
            ExecuteProgram(program);
        stable.CopyAllFrom(unstable);

        Y = Number.DEFAULT_Y;
    }

    public void Stabilize()
    {
        unstable.CopyFrom(stable);
    }

    public void ExecuteProgram(Program program)
    {
        ExecuteProgram(program, stack);
    }

    public void ExecuteProgram(Program program, ProgramStack stack)
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

        if (stackPointer.Address > Constants.MAX_STACK_DEPTH)
            throw InterpreterError("Stack overflow protection tripped! (stack pointer too high)");

        if (++depth > Constants.MAX_RECURSION_DEPTH)
            throw InterpreterError("Stack overflow protection tripped! (exceeded max depth)");
        // defer --depth;

        for (CodeAddress c = 0; c < program.Length; c++)
        {
            switch ((InstructionKind)program[c])
            {
                case InstructionKind.Start:
                    break;
                case InstructionKind.End:
                    if (c != program.Length - 1)
                        throw InterpreterError("Unexpected program end!");

                    goto case InstructionKind.Return;
                case InstructionKind.Return:
                    if (stackPointer != stack.Count - program.Arity)
                        throw InterpreterError("Bad stack pointer value!");

                    --depth;
                    return;
                case InstructionKind.LoadIn:
                    stack.Push(X);
                    break;
                case InstructionKind.StoreIn:
                    X = stack.Pop();
                    break;
                case InstructionKind.LoadOut:
                    stack.Push(Y);
                    break;
                case InstructionKind.StoreOut:
                    Y = stack.Pop();
                    break;
                case InstructionKind.LoadNumber:
                    DataAddress dAddress = (DataAddress)program.ExtractAddress(ref c);
                    stack.Push(program[dAddress]);
                    break;
                case InstructionKind.LoadPersistent:
                    {
                        MemoryAddress loadAddress = (MemoryAddress)program.ExtractAddress(ref c);
                        stack.Push(unstable.GetPersistent(loadAddress));
                    }
                    break;
                case InstructionKind.StorePersistent:
                    {
                        MemoryAddress storeAddress = (MemoryAddress)program.ExtractAddress(ref c);
                        unstable.SetPersistent(storeAddress, stack.Pop());
                    }
                    break;
                case InstructionKind.LoadImpersistent:
                    {
                        MemoryAddress loadAddress = (MemoryAddress)program.ExtractAddress(ref c);
                        stack.Push(unstable.GetImpersistent(loadAddress));
                    }
                    break;
                case InstructionKind.StoreImpersistent:
                    {
                        MemoryAddress storeAddress = (MemoryAddress)program.ExtractAddress(ref c);
                        unstable.SetImpersistent(storeAddress, stack.Pop());
                    }
                    break;
                case InstructionKind.LoadStack:
                    {
                        StackAddress loadAddress = (StackAddress)program.ExtractAddress(ref c);
                        stack.Push(stack[stackPointer + loadAddress]);
                    }
                    break;
                case InstructionKind.StoreStack:
                    {
                        StackAddress storeAddress = (StackAddress)program.ExtractAddress(ref c);
                        stack[stackPointer + storeAddress] = stack.Pop();
                    }
                    break;
                case InstructionKind.Swap:
                    Debug.Assert(stack.Count >= 2);

                    Number swap1 = stack.Pop();
                    Number swap2 = stack.Pop();
                    stack.Push(swap1);
                    stack.Push(swap2);
                    break;
                case InstructionKind.LoadZero:
                    stack.Push(Number.ZERO);
                    break;
                case InstructionKind.LoadE:
                    stack.Push(E);
                    break;
                case InstructionKind.LoadPi:
                    stack.Push(PI);
                    break;
                case InstructionKind.LoadTau:
                    stack.Push(Tau);
                    break;
                case InstructionKind.LoadCapacity:
                    stack.Push(Constants.LUT_POINTS_CAPACITY);
                    break;
                case InstructionKind.Jmp:
                    CodeAddress jmpAddress = (CodeAddress)program.ExtractAddress(ref c);
                    c = jmpAddress;
                    break;
                case InstructionKind.Jz:
                    CodeAddress jzAddress = (CodeAddress)program.ExtractAddress(ref c);
                    if (!stack.Pop())
                        c = jzAddress;
                    break;
                case InstructionKind.Call:
                    MemoryAddress functionAddress = (MemoryAddress)program.ExtractAddress(ref c);
                    Program function = functions[functionAddress];

                    Number y = Y;
                    StackAddress tempStackPointer = stackPointer;
                    stackPointer = stack.Count - function.Arity;

                    ExecuteProgram(function);

                    stack.RemoveRange(stackPointer.Address, function.Arity);
                    stackPointer = tempStackPointer;
                    stack.Push(Y);
                    Y = y;
                    break;
                case InstructionKind.Add:
                    Fn2((y, x) => x + y);
                    break;
                case InstructionKind.Sub:
                    Fn2((y, x) => x - y);
                    break;
                case InstructionKind.Mul:
                    Fn2((y, x) => x * y);
                    break;
                case InstructionKind.Div:
                    Fn2((y, x) => x / y);
                    break;
                case InstructionKind.Mod:
                    Fn2((y, x) => x % y);
                    break;
                case InstructionKind.Pow:
                    Fn2((y, x) => Pow(x, y));
                    break;
                case InstructionKind.Exp: // implicit first argument
                    Fn1(a => Exp(a));
                    break;
                case InstructionKind.Or:
                    Fn2((y, x) => x | y);
                    break;
                case InstructionKind.And:
                    Fn2((y, x) => x & y);
                    break;
                case InstructionKind.Lt:
                    Fn2((y, x) => x < y);
                    break;
                case InstructionKind.Gt:
                    Fn2((y, x) => x > y);
                    break;
                case InstructionKind.Le:
                    Fn2((y, x) => x <= y);
                    break;
                case InstructionKind.Ge:
                    Fn2((y, x) => x >= y);
                    break;
                case InstructionKind.Eq:
                    Fn2((y, x) => x == y);
                    break;
                case InstructionKind.Ne:
                    Fn2((y, x) => x != y);
                    break;
                case InstructionKind.Not: // unary
                    stack.Push(!stack.Pop());
                    break;
                case InstructionKind.Abs:
                    Fn1(a => Abs(a));
                    break;
                case InstructionKind.Sign:
                    Fn1(a => Sign(a));
                    break;
                case InstructionKind.CopySign:
                    Fn2((b, a) => CopySign(a, b));
                    break;
                case InstructionKind.Round:
                    Fn1(a => Round(a));
                    break;
                case InstructionKind.Trunc:
                    Fn1(a => Truncate(a));
                    break;
                case InstructionKind.Floor:
                    Fn1(a => Floor(a));
                    break;
                case InstructionKind.Ceil:
                    Fn1(a => Ceiling(a));
                    break;
                case InstructionKind.Clamp:
                    Fn3((c, b, a) => Clamp(a, b, c));
                    break;
                case InstructionKind.Min:
                    Fn2((b, a) => Min(a, b));
                    break;
                case InstructionKind.Max:
                    Fn2((b, a) => Max(a, b));
                    break;
                case InstructionKind.MinM:
                    Fn2((b, a) => MinMagnitude(a, b));
                    break;
                case InstructionKind.MaxM:
                    Fn2((b, a) => MaxMagnitude(a, b));
                    break;
                case InstructionKind.Sqrt:
                    Fn1(a => Sqrt(a));
                    break;
                case InstructionKind.Cbrt:
                    Fn1(a => Cbrt(a));
                    break;
                case InstructionKind.Log:
                    Fn1(a => Log(a));
                    break;
                case InstructionKind.Log2:
                    Fn1(a => Log2(a));
                    break;
                case InstructionKind.Log10:
                    Fn1(a => Log10(a));
                    break;
                case InstructionKind.LogB:
                    Fn2((b, a) => Log(a, b));
                    break;
                case InstructionKind.ILogB:
                    Fn1(a => ILogB(a));
                    break;
                case InstructionKind.Sin:
                    Fn1(a => Sin(a));
                    break;
                case InstructionKind.Sinh:
                    Fn1(a => Sinh(a));
                    break;
                case InstructionKind.Asin:
                    Fn1(a => Asin(a));
                    break;
                case InstructionKind.Asinh:
                    Fn1(a => Asinh(a));
                    break;
                case InstructionKind.Cos:
                    Fn1(a => Cos(a));
                    break;
                case InstructionKind.Cosh:
                    Fn1(a => Cosh(a));
                    break;
                case InstructionKind.Acos:
                    Fn1(a => Acos(a));
                    break;
                case InstructionKind.Acosh:
                    Fn1(a => Acosh(a));
                    break;
                case InstructionKind.Tan:
                    Fn1(a => Tan(a));
                    break;
                case InstructionKind.Tanh:
                    Fn1(a => Tanh(a));
                    break;
                case InstructionKind.Atan:
                    Fn1(a => Atan(a));
                    break;
                case InstructionKind.Atanh:
                    Fn1(a => Atanh(a));
                    break;
                case InstructionKind.Atan2:
                    Fn2((b, a) => Atan2(a, b));
                    break;
                case InstructionKind.FusedMultiplyAdd:
                    Fn3((c, b, a) => FusedMultiplyAdd(a, b, c));
                    break;
                case InstructionKind.ScaleB:
                    Fn2((b, a) => ScaleB(a, (int)b)); // lol
                    break;
                default:
                    throw InterpreterError("Not an instruction!");
            }
        }

        throw InterpreterError("Program loop exited without returning!");
    }

    private static InterpreterException InterpreterError(string error)
    {
        return new InterpreterException(error);
    }
}
