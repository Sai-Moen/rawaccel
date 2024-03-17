using scripting.Common;
using scripting.Syntactical;

namespace scripting.Interpretation;

/// <summary>
/// Executes Programs.
/// </summary>
public class Interpreter : IInterpreter
{
    #region Fields

    private Number x = Number.DEFAULT_X;
    private Number y = Number.DEFAULT_Y;

    private readonly Dictionary<string, MemoryAddress> addresses = new();

    private readonly MemoryHeap stable;
    private readonly MemoryHeap unstable;
    private readonly Stack<Number> mainStack = new();

    private readonly Program mainProgram;
    private readonly Program[] startup;

    private Parameters _settings = new();

    #endregion Fields

    #region Constructors

    /// <summary>
    /// Initializes the script and its default settings.
    /// </summary>
    /// <param name="syntactic">result of syntactic analysis</param>
    /// <exception cref="InterpreterException"/>
    public Interpreter(ParsingResult syntactic)
    {
        Description = syntactic.Description;

        int numParameters = syntactic.Parameters.Count;
        Debug.Assert(numParameters <= Constants.MAX_PARAMETERS);
        for (int i = 0; i < numParameters; i++)
        {
            MemoryAddress address = i;
            addresses.Add(syntactic.Parameters[i].Name, address);
        }

        int numVariables = syntactic.Variables.Count;
        int capacity = Constants.MAX_PARAMETERS + numVariables;
        stable = new(capacity);
        unstable = new(capacity);

        startup = new Program[numVariables];

        Debug.Assert(numVariables <= Constants.MAX_VARIABLES);
        for (int i = 0; i < numVariables; i++)
        {
            MemoryAddress address = Constants.MAX_PARAMETERS + i;
            VariableAssignment variable = syntactic.Variables[i];
            addresses.Add(variable.Name, address);
            startup[i] = new(variable.Expr, addresses);
        }

        mainProgram = new(syntactic.Tokens, addresses);

        // responsibility to change settings from script defaults to saved settings is on the caller
        Defaults = syntactic.Parameters;
        Settings = Defaults; // make sure startup is initialized before this
    }

    #endregion Constructors

    #region Properties

    public string Description { get; private set; }

    public Parameters Defaults { get; }

    public Parameters Settings
    {
        get
        {
            return _settings;
        }

        set
        {
            _settings = value;
            Init();
        }
    }

    #endregion Properties

    #region Methods

    public double Calculate(double x)
    {
        this.x = x;
        Exec(mainProgram, mainStack);
        double y = this.y;

        Reset();
        return y;
    }

    private void Init()
    {
        // Load Parameters to Stable
        foreach (ParameterAssignment parameter in Settings)
        {
            stable[addresses[parameter.Name]] = parameter.Value;
        }

        // Load Parameters to Volatile
        Reset();

        // Do startup tasks
        //      Synchronization is not used in Exec,
        //      as each thread should only be able to do independent work on their own address.
        _ = Parallel.ForEach(startup, p => Exec(p, new()));

        // Load everything to Stable
        stable.CopyFrom(unstable);
    }

    private void Reset()
    {
        unstable.CopyFrom(stable);
        y = Number.DEFAULT_Y;
    }

    private void Exec(Program program, Stack<Number> stack)
    {
        void Fn1(Func<Number, Number> func)
        {
            Debug.Assert(stack.Count >= 1);
            stack.Push(func(stack.Pop()));
        }

        void Fn2(Func<Number, Number, Number> func)
        {
            Debug.Assert(stack.Count >= 2);
            stack.Push(func(stack.Pop(), stack.Pop()));
        }

        void Fn3(Func<Number, Number, Number, Number> func)
        {
            Debug.Assert(stack.Count >= 3);
            stack.Push(func(stack.Pop(), stack.Pop(), stack.Pop()));
        }

        for (CodeAddress i = 0; i < program.Length; i++)
            switch (program[i].instruction.Type)
            {
                case InstructionType.Start:
                    break;
                case InstructionType.End:
                    if (i != program.Length - 1)
                    {
                        InterpreterError("Unexpected program end!");
                    }
                    Debug.Assert(stack.Count == 0);
                    return;
                case InstructionType.Load:
                    MemoryAddress loadAddress = (MemoryAddress)program.GetOperandFromNext(ref i);
                    stack.Push(unstable[loadAddress]);
                    break;
                case InstructionType.Store:
                    MemoryAddress storeAddress = (MemoryAddress)program.GetOperandFromNext(ref i);
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
                    DataAddress dAddress = (DataAddress)program.GetOperandFromNext(ref i);
                    stack.Push(program.GetData(dAddress));
                    break;
                case InstructionType.Swap:
                    Number swap1 = stack.Pop();
                    Number swap2 = stack.Pop();
                    stack.Push(swap1);
                    stack.Push(swap2);
                    break;
                case InstructionType.Jmp:
                    CodeAddress jmpAddress = (CodeAddress)program.GetOperandFromNext(ref i);
                    i = jmpAddress;
                    break;
                case InstructionType.Jz:
                    CodeAddress jzAddress = (CodeAddress)program.GetOperandFromNext(ref i);
                    if (!stack.Pop())
                    {
                        i = jzAddress;
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

    #endregion Methods

    private static void InterpreterError(string error)
    {
        throw new InterpreterException(error);
    }
}
