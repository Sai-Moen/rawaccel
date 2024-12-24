using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using userspace_backend.ScriptingLanguage.Compiler;
using userspace_backend.ScriptingLanguage.Compiler.CodeGen;
using userspace_backend.ScriptingLanguage.Compiler.Parser;
using userspace_backend.ScriptingLanguage.Compiler.Tokenizer;
using userspace_backend.ScriptingLanguage.Script;
using static System.Math;

namespace userspace_backend.ScriptingLanguage.Interpreter;

/// <summary>
/// Executes Programs.
/// </summary>
public class InterpreterImpl : IInterpreter
{
    #region Fields

    private readonly Dictionary<string, MemoryAddress> assignmentAddresses = [];
    private readonly Program[] assignments;
    private readonly MemoryHeap stable = new();
    private readonly MemoryHeap unstable = new();

    private readonly Dictionary<string, MemoryAddress> functionAddresses = [];
    private readonly Program[] functions;

    private StackAddress stackPointer;
    private readonly ProgramStack stack = [];

    private int depth;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes the script and its default settings.
    /// </summary>
    /// <param name="parsed">Result of parsing.</param>
    /// <exception cref="InterpreterException"/>
    public InterpreterImpl(ParsingResult parsed)
    {
        CompilerContext context = parsed.Context;
        Description = parsed.Description;

        EmitterImpl emitter = new(context, assignmentAddresses, functionAddresses);
        int numPersistent = 0;
        int numImpersistent = 0;

        Parameters parameters = parsed.Parameters;
        int numParameters = parameters.Count;

        Debug.Assert(numParameters <= Constants.MAX_PARAMETERS);
        foreach (Parameter parameter in parameters)
            assignmentAddresses.Add(parameter.Name, (MemoryAddress)numPersistent++);

        IList<ASTNode> declarations = parsed.Declarations;
        int numDeclarations = declarations.Count;

        List<Program> assignmentsList = new(numDeclarations);
        List<Program> functionsList = new(numDeclarations);
        int numFunctions = 0;

        Debug.Assert(numDeclarations <= Constants.MAX_DECLARATIONS);
        foreach (ASTNode ast in declarations)
        {
            ASTUnion union = ast.Union;
            switch (ast.Tag)
            {
                case ASTTag.Assign:
                    ASTAssign assignment = union.astAssign;
                    {
                        Token identifier = assignment.Identifier;
                        MemoryAddress address = identifier.Type switch
                        {
                            TokenType.Immutable or TokenType.Persistent => (MemoryAddress)numPersistent++,
                            TokenType.Impersistent => (MemoryAddress)numImpersistent++,

                            _ => throw InterpreterError("Identifier does not have the correct type for a variable!")
                        };
                        assignmentAddresses.Add(context.GetSymbol(identifier), address);

                        assignmentsList.Add(emitter.Emit([ast]));
                    }
                    break;
                case ASTTag.Function:
                    ASTFunction function = union.astFunction;
                    {
                        Token identifier = function.Identifier;
                        MemoryAddress address = (MemoryAddress)numFunctions++;
                        functionAddresses.Add(context.GetSymbol(identifier), address);

                        functionsList.Add(emitter.EmitFunction(function.Args, function.Code));
                    }
                    break;
                default:
                    throw InterpreterError("Invalid AST node for a declaration!");
            }
        }

        assignments = [.. assignmentsList];
        functions = [.. functionsList];

        stable.EnsureSizes(numPersistent, numImpersistent);
        unstable.EnsureSizes(numPersistent, numImpersistent);

        IList<ParsedCallback> callbacks = parsed.Callbacks;
        Debug.Assert(callbacks.Count > 0);

        Callbacks = new(this, callbacks[0], emitter);
        foreach (ParsedCallback cb in callbacks)
            Callbacks.Add(cb, emitter);

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
            stable.SetPersistent(assignmentAddresses[parameter.Name], parameter.Value);
        }
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
            switch ((InstructionType)program[c])
            {
                case InstructionType.Start:
                    break;
                case InstructionType.End:
                    if (c != program.Length - 1)
                        throw InterpreterError("Unexpected program end!");

                    goto case InstructionType.Return;
                case InstructionType.Return:
                    if (stackPointer != stack.Count - program.Arity)
                        throw InterpreterError("Bad stack pointer value!");

                    --depth;
                    return;
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
                case InstructionType.LoadStack:
                    {
                        StackAddress loadAddress = (StackAddress)program.ExtractAddress(ref c);
                        stack.Push(stack[stackPointer + loadAddress]);
                    }
                    break;
                case InstructionType.StoreStack:
                    {
                        StackAddress storeAddress = (StackAddress)program.ExtractAddress(ref c);
                        stack[stackPointer + storeAddress] = stack.Pop();
                    }
                    break;
                case InstructionType.Swap:
                    Debug.Assert(stack.Count >= 2);

                    Number swap1 = stack.Pop();
                    Number swap2 = stack.Pop();
                    stack.Push(swap1);
                    stack.Push(swap2);
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
                case InstructionType.Jmp:
                    CodeAddress jmpAddress = (CodeAddress)program.ExtractAddress(ref c);
                    c = jmpAddress;
                    break;
                case InstructionType.Jz:
                    CodeAddress jzAddress = (CodeAddress)program.ExtractAddress(ref c);
                    if (!stack.Pop())
                        c = jzAddress;
                    break;
                case InstructionType.Call:
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
        }

        throw InterpreterError("Program loop exited without returning!");
    }

    #endregion

    private static InterpreterException InterpreterError(string error)
    {
        return new InterpreterException(error);
    }
}
