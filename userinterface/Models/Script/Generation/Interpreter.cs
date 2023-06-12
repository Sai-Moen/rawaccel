using System;
using System.Collections.Generic;
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

        private ParameterPairs _settings = new();

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
            List<ParameterAssignment> parameters,
            List<VariableAssignment> variables,
            TokenList code)
        {
            Debug.Assert(parameters.Count <= Parsing.MAX_PARAMETERS);
            for (int i = 0; i < parameters.Count; i++)
            {
                MemoryAddress address = i;

                ParameterAssignment parameter = parameters[i];

                string name = parameter.Token.Base.Symbol;

                Addresses.Add(name, address);

                double value = parameter.Value ??
                    throw new InterpreterException(parameter.Token.Line, "Parameter Value not set!");

                Defaults[i] = new(name, value);
            }

            Startup = new Program[variables.Count];

            Debug.Assert(variables.Count <= Parsing.MAX_VARIABLES);
            for (int i = 0; i < variables.Count; i++)
            {
                MemoryAddress address = i + Parsing.MAX_PARAMETERS;

                VariableAssignment variable = variables[i];

                string name = variable.Token.Base.Symbol;

                Addresses.Add(name, address);

                Expression expr = variable.Expr ??
                    throw new InterpreterException(variable.Token.Line, "Variable Expr not set!");

                Startup[i] = new(expr, Addresses);
            }

            MainProgram = new(code, Addresses);

            Settings = Defaults; // Make sure Startup is initialized before this
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// The parameters and their default values, according to the script.
        /// </summary>
        public ParameterPairs Defaults { get; } = new();

        /// <summary>
        /// The current values of all parameters.
        /// Setting this property will automatically update the values of all variables.
        /// </summary>
        public ParameterPairs Settings
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
            Debug.Assert(MainStack.Count == 0);

            double y = Y;
            Restore();

            Y = Number.DefaultY;
            return y;
        }

        private void Init()
        {
            foreach (ParameterPairs.ParameterNameValue? pair in Settings)
            {
                if (pair.HasValue)
                {
                    ParameterPairs.ParameterNameValue value = pair.Value;
                    
                    Stable[Addresses[value.Name]] = value.Value;
                }
            }

            // Load Parameters to Volatile
            Restore();

            /*
             * The reasons this can be done multithreaded are:
             *   variable expressions can only read
             *     numbers, constants and parameters (which are already here),
             *   variable expressions can only write
             *     to themselves and not other variables.
             * This should be checked in the parser, not the interpreter,
             *   so that's why Exec seems to have access to all instructions at this point,
             *   but the startup code shouldn't.
             */
            Parallel.ForEach(Startup, (p) => Exec(p, new()));

            // Load everything to Stable
            Stable.RestoreTo(Volatile);
        }

        private void Exec(Program program, ProgramStack stack)
        {
            for (int i = 0; i < program.Instructions.Count; i++)
            {
                Instruction instruction = program.Instructions[i];
                InstructionType type = (InstructionType)instruction[0];
                switch (type)
                {
                    case InstructionType.Start:
                        break;
                    case InstructionType.End:
                        if (i != program.Instructions.Count - 1)
                        {
                            InterpreterError("Unexpected program end!");
                        }

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
                        if (stack.Pop() == 0)
                        {
                            i = jzAddress;
                        }

                        break;
                    case InstructionType.LoadE:
                    case InstructionType.LoadPi:
                    case InstructionType.LoadTau:
                    case InstructionType.LoadZero:
                        stack.Push(Lookup(type, 0, 0));
                        break;
                    case InstructionType.Add:
                    case InstructionType.Sub:
                    case InstructionType.Mul:
                    case InstructionType.Div:
                    case InstructionType.Mod:
                    case InstructionType.Exp:
                    case InstructionType.Or:
                    case InstructionType.And:
                    case InstructionType.Lt:
                    case InstructionType.Gt:
                    case InstructionType.Le:
                    case InstructionType.Ge:
                    case InstructionType.Eq:
                    case InstructionType.Ne:
                        stack.Push(Lookup(type, stack.Pop(), stack.Pop()));
                        break;
                    case InstructionType.ExpE: // implicit first argument
                    case InstructionType.Not: // unary
                    case InstructionType.Abs:
                    case InstructionType.Sqrt:
                    case InstructionType.Cbrt:
                    case InstructionType.Round:
                    case InstructionType.Trunc:
                    case InstructionType.Ceil:
                    case InstructionType.Floor:
                    case InstructionType.Log:
                    case InstructionType.Log2:
                    case InstructionType.Log10:
                    case InstructionType.Sin:
                    case InstructionType.Sinh:
                    case InstructionType.Asin:
                    case InstructionType.Asinh:
                    case InstructionType.Cos:
                    case InstructionType.Cosh:
                    case InstructionType.Acos:
                    case InstructionType.Acosh:
                    case InstructionType.Tan:
                    case InstructionType.Tanh:
                    case InstructionType.Atan:
                    case InstructionType.Atanh:
                        stack.Push(Lookup(type, 0, stack.Pop()));
                        break;
                    case InstructionType.Count:
                    default:
                        InterpreterError("Not an instruction!");
                        break;
                }
            }
        }

        private static Number Lookup(InstructionType type, Number y, Number x)
        {
            // Arguments are reversed because of stack pop order,
            // In postfix notation, rhs comes before lhs on the stack.
            return type switch
            {
                InstructionType.LoadE       => Math.E,
                InstructionType.LoadPi      => Math.PI,
                InstructionType.LoadTau     => Math.Tau,
                InstructionType.LoadZero    => 0,

                InstructionType.Add         => x + y,
                InstructionType.Sub         => x - y,
                InstructionType.Mul         => x * y,
                InstructionType.Div         => x / y,
                InstructionType.Mod         => x % y,
                InstructionType.Exp         => Math.Pow(x, y),
                InstructionType.ExpE        => Math.Exp(x),

                InstructionType.Or          => x | y,
                InstructionType.And         => x & y,
                InstructionType.Lt          => x < y,
                InstructionType.Gt          => x > y,
                InstructionType.Le          => x <= y,
                InstructionType.Ge          => x >= y,
                InstructionType.Eq          => x == y,
                InstructionType.Ne          => x != y,
                InstructionType.Not         => !x,

                InstructionType.Abs         => Math.Abs(x),
                InstructionType.Sqrt        => Math.Sqrt(x),
                InstructionType.Cbrt        => Math.Cbrt(x),
                InstructionType.Round       => Math.Round(x),
                InstructionType.Trunc       => Math.Truncate(x),
                InstructionType.Ceil        => Math.Ceiling(x),
                InstructionType.Floor       => Math.Floor(x),
                InstructionType.Log         => Math.Log(x),
                InstructionType.Log2        => Math.Log2(x),
                InstructionType.Log10       => Math.Log10(x),
                InstructionType.Sin         => Math.Sin(x),
                InstructionType.Sinh        => Math.Sinh(x),
                InstructionType.Asin        => Math.Asin(x),
                InstructionType.Asinh       => Math.Asinh(x),
                InstructionType.Cos         => Math.Cos(x),
                InstructionType.Cosh        => Math.Cosh(x),
                InstructionType.Acos        => Math.Acos(x),
                InstructionType.Acosh       => Math.Acosh(x),
                InstructionType.Tan         => Math.Tan(x),
                InstructionType.Tanh        => Math.Tanh(x),
                InstructionType.Atan        => Math.Atan(x),
                InstructionType.Atanh       => Math.Atanh(x),

                _ => throw new InterpreterException("Not a function!"),
            };
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
