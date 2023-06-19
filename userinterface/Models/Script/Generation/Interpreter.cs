using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace userinterface.Models.Script.Generation
{
    /// <summary>
    /// Executes Programs.
    /// </summary>
    public class Interpreter
    {
        #region Fields

        private Number X = Number.DefaultX;

        private Number Y = Number.DefaultY;

        private readonly MemoryMap Addresses = new();

        private readonly MemoryHeap Stable = new();

        private readonly MemoryHeap Volatile = new();

        private readonly ProgramStack MainStack = new();

        private readonly Program MainProgram;

        private readonly Program[] Startup;

        private Parameters _settings = new();

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes the script and its default settings.
        /// </summary>
        /// <param name="parameters">The Parameters of the script with their default values.</param>
        /// <param name="variables">The Variables of the script, with their Expression.</param>
        /// <param name="code">The main code of the calculation block.</param>
        /// <exception cref="InterpreterException">Thrown on execution failure.</exception>
        public Interpreter(
            Parameters parameters,
            Variables variables,
            TokenList code)
        {
            Debug.Assert(parameters.Count <= Constants.MAX_PARAMETERS);
            for (int i = 0; i < parameters.Count; i++)
            {
                MemoryAddress address = i;
                Addresses.Add(parameters[i].Name, address);
            }

            Defaults = parameters;

            Startup = new Program[variables.Count];

            Debug.Assert(variables.Count <= Constants.MAX_VARIABLES);
            for (int i = 0; i < variables.Count; i++)
            {
                MemoryAddress address = i + Constants.MAX_PARAMETERS;
                VariableAssignment variable = variables[i];
                Addresses.Add(variable.Name, address);
                Startup[i] = new(variable.Expr, Addresses);
            }

            MainProgram = new(code, Addresses);

            Settings = Defaults; // Make sure Startup is initialized before this
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// The parameters and their default values, according to the script.
        /// </summary>
        public Parameters Defaults { get; } = new();

        /// <summary>
        /// The current values of all parameters.
        /// Setting this property will automatically update the values of all variables.
        /// </summary>
        public Parameters Settings
        {
            get { return _settings; }
            set
            {
                _settings = value;
                Init();
            }
        }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Performs a calculation on the currently loaded script and settings.
        /// </summary>
        /// <param name="x">The input value to inject into the loaded script and settings.</param>
        /// <returns>The resulting output value.</returns>
        public double Calculate(double x)
        {
            X = x;
            Exec(MainProgram, MainStack);

            double y = Y;
            Y = Number.DefaultY;

            Restore();
            return y;
        }

        private void Init()
        {
            // Load Parameters to Stable
            foreach (ParameterAssignment parameter in Settings)
            {
                Stable[Addresses[parameter.Name]] = parameter.Value;
            }

            // Load Parameters to Volatile
            Restore();

            // Do startup tasks
            // If startup tasks modified something other than their own address,
            // that is a bug in the parser, not here.
            Parallel.ForEach(Startup, (p) => Exec(p, new()));

            // Load everything to Stable
            Stable.RestoreTo(Volatile);
        }

        private void Exec(Program program, ProgramStack stack)
        {
            for (int i = 0; i < program.Instructions.Count; i++)
            {
                Instruction instruction = program.Instructions[i];
                switch (instruction.Type)
                {
                    case InstructionType.Start:
                        break;
                    case InstructionType.End:
                        if (i != program.Instructions.Count - 1)
                        {
                            InterpreterError("Unexpected program end!");
                        }

                        Debug.Assert(stack.Count == 0);
                        return;
                    case InstructionType.Load:
                        MemoryAddress loadAddress = instruction;
                        stack.Push(Volatile[loadAddress]);
                        break;
                    case InstructionType.Store:
                        MemoryAddress storeAddress = instruction;
                        Volatile[storeAddress] = stack.Pop();
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
                        Number number = instruction;
                        stack.Push(number);
                        break;
                    case InstructionType.Swap:
                        Number swap1 = stack.Pop();
                        Number swap2 = stack.Pop();
                        stack.Push(swap1);
                        stack.Push(swap2);
                        break;
                    case InstructionType.Jmp:
                        CodeAddress jmpAddress = instruction;
                        i = jmpAddress;
                        break;
                    case InstructionType.Jz:
                        CodeAddress jzAddress = instruction;
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
                        stack.Push(Number.Zero);
                        break;
                    case InstructionType.Add:
                        stack.Push(Op2((x, y) => x + y, stack.Pop(), stack.Pop()));
                        break;
                    case InstructionType.Sub:
                        stack.Push(Op2((x, y) => x - y, stack.Pop(), stack.Pop()));
                        break;
                    case InstructionType.Mul:
                        stack.Push(Op2((x, y) => x * y, stack.Pop(), stack.Pop()));
                        break;
                    case InstructionType.Div:
                        stack.Push(Op2((x, y) => x / y, stack.Pop(), stack.Pop()));
                        break;
                    case InstructionType.Mod:
                        stack.Push(Op2((x, y) => x % y, stack.Pop(), stack.Pop()));
                        break;
                    case InstructionType.Exp:
                        stack.Push(Op2((x, y) => Math.Pow(x, y), stack.Pop(), stack.Pop()));
                        break;
                    case InstructionType.ExpE: // implicit first argument
                        stack.Push(Math.Exp(stack.Pop()));
                        break;
                    case InstructionType.Or:
                        stack.Push(Op2((x, y) => x | y, stack.Pop(), stack.Pop()));
                        break;
                    case InstructionType.And:
                        stack.Push(Op2((x, y) => x & y, stack.Pop(), stack.Pop()));
                        break;
                    case InstructionType.Lt:
                        stack.Push(Op2((x, y) => x < y, stack.Pop(), stack.Pop()));
                        break;
                    case InstructionType.Gt:
                        stack.Push(Op2((x, y) => x > y, stack.Pop(), stack.Pop()));
                        break;
                    case InstructionType.Le:
                        stack.Push(Op2((x, y) => x <= y, stack.Pop(), stack.Pop()));
                        break;
                    case InstructionType.Ge:
                        stack.Push(Op2((x, y) => x >= y, stack.Pop(), stack.Pop()));
                        break;
                    case InstructionType.Eq:
                        stack.Push(Op2((x, y) => x == y, stack.Pop(), stack.Pop()));
                        break;
                    case InstructionType.Ne:
                        stack.Push(Op2((x, y) => x != y, stack.Pop(), stack.Pop()));
                        break;
                    case InstructionType.Not: // unary
                        stack.Push(!stack.Pop());
                        break;
                    case InstructionType.Abs:
                        stack.Push(Math.Abs(stack.Pop()));
                        break;
                    case InstructionType.Sqrt:
                        stack.Push(Math.Sqrt(stack.Pop()));
                        break;
                    case InstructionType.Cbrt:
                        stack.Push(Math.Cbrt(stack.Pop()));
                        break;
                    case InstructionType.Round:
                        stack.Push(Math.Round(stack.Pop()));
                        break;
                    case InstructionType.Trunc:
                        stack.Push(Math.Truncate(stack.Pop()));
                        break;
                    case InstructionType.Ceil:
                        stack.Push(Math.Ceiling(stack.Pop()));
                        break;
                    case InstructionType.Floor:
                        stack.Push(Math.Floor(stack.Pop()));
                        break;
                    case InstructionType.Log:
                        stack.Push(Math.Log(stack.Pop()));
                        break;
                    case InstructionType.Log2:
                        stack.Push(Math.Log2(stack.Pop()));
                        break;
                    case InstructionType.Log10:
                        stack.Push(Math.Log10(stack.Pop()));
                        break;
                    case InstructionType.Sin:
                        stack.Push(Math.Sin(stack.Pop()));
                        break;
                    case InstructionType.Sinh:
                        stack.Push(Math.Sinh(stack.Pop()));
                        break;
                    case InstructionType.Asin:
                        stack.Push(Math.Asin(stack.Pop()));
                        break;
                    case InstructionType.Asinh:
                        stack.Push(Math.Asinh(stack.Pop()));
                        break;
                    case InstructionType.Cos:
                        stack.Push(Math.Cos(stack.Pop()));
                        break;
                    case InstructionType.Cosh:
                        stack.Push(Math.Cosh(stack.Pop()));
                        break;
                    case InstructionType.Acos:
                        stack.Push(Math.Acos(stack.Pop()));
                        break;
                    case InstructionType.Acosh:
                        stack.Push(Math.Acosh(stack.Pop()));
                        break;
                    case InstructionType.Tan:
                        stack.Push(Math.Tan(stack.Pop()));
                        break;
                    case InstructionType.Tanh:
                        stack.Push(Math.Tanh(stack.Pop()));
                        break;
                    case InstructionType.Atan:
                        stack.Push(Math.Atan(stack.Pop()));
                        break;
                    case InstructionType.Atanh:
                        stack.Push(Math.Atanh(stack.Pop()));
                        break;
                    case InstructionType.Count:
                    default:
                        InterpreterError("Not an instruction!");
                        break;
                }
            }
        }

        private static Number Op2(Func<Number, Number, Number> func, Number right, Number left)
        {
            // Reversed method args because of stack unwinding in reverse ^^
            return func(left, right);
        }

        private void Restore()
        {
            Volatile.RestoreTo(Stable);
        }

        #endregion Methods

        private static void InterpreterError(string error)
        {
            throw new InterpreterException(error);
        }
    }

    public class InterpreterException : ScriptException
    {
        public InterpreterException(string message) : base(message) { }

        public InterpreterException(uint line, string message) : base($"Line {line}: {message}") { }
    }
}
