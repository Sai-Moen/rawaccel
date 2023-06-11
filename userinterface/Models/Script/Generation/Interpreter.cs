using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace userinterface.Models.Script.Generation
{
    public class Interpreter
    {
        #region Fields

        private Number X = new(0);

        private Number Y = new(1);

        private readonly MemoryMap Addresses = new();

        private readonly MemoryHeap Stable = new();

        private readonly MemoryHeap Volatile = new();

        private readonly ProgramStack MainStack = new();

        private readonly Program MainProgram;

        private readonly Program[] Startup;

        private ParameterPairs _settings = new();

        #endregion Fields

        #region Constructors

        public Interpreter(
            List<ParameterAssignment> parameters,
            List<VariableAssignment> variables,
            TokenList code)
        {
            Debug.Assert(parameters.Count <= Parsing.MAX_PARAMETERS);
            for (int i = 0; i < parameters.Count; i++)
            {
                MemoryAddress address = new(i);

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
                MemoryAddress address = new(i + Parsing.MAX_PARAMETERS);

                VariableAssignment variable = variables[i];

                string name = variable.Token.Base.Symbol;

                Addresses.Add(name, address);

                Expression expr = variable.Expr ??
                    throw new InterpreterException(variable.Token.Line, "Variable Expr not set!");

                Startup[i] = new(expr, Addresses);
            }

            MainProgram = new(new(code), Addresses);

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

        public double Calculate(double x)
        {
            X = new(x);

            Exec(MainProgram, MainStack);
            Debug.Assert(MainStack.Count == 0);

            double y = Y.Value;
            Restore();

            Y = new(1);
            return y;
        }

        private void Init()
        {
            foreach (ParameterPairs.ParameterNameValue? pair in Settings)
            {
                if (pair.HasValue)
                {
                    ParameterPairs.ParameterNameValue value = pair.Value;
                    
                    Stable[new(Addresses[value.Name].Address)] = value.Value;
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
            InitStable();
        }

        private void Exec(Program program, ProgramStack stack)
        {
            for (int i = 0; i < program.Instructions.Count; i++)
            {
                Instruction instruction = program.Instructions[i];
                InstructionType type = (InstructionType)instruction[0];

                byte tableIndex = type.ToByte();
                (Number, Number) tableArgs = (Number.Zero, Number.Zero); // 0 means ignore

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
                        MemoryAddress loadAddress = new(instruction);
                        stack.Push(new(Volatile[loadAddress]));
                        break;
                    case InstructionType.Store:
                        MemoryAddress storeAddress = new(instruction);
                        Volatile[storeAddress] = stack.Pop().Value;
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
                        Number number = new(instruction);
                        stack.Push(number);
                        break;
                    case InstructionType.Swap:
                        Number swap1 = stack.Pop();
                        Number swap2 = stack.Pop();

                        stack.Push(swap1);
                        stack.Push(swap2);
                        break;
                    case InstructionType.Jmp:
                        CodeAddress jmpAddress = new(instruction);
                        i = jmpAddress.Address;
                        break;
                    case InstructionType.Jz:
                        CodeAddress jzAddress = new(instruction);
                        if (stack.Pop().Value == 0)
                        {
                            i = jzAddress.Address;
                        }

                        break;
                    case InstructionType.LoadE:
                    case InstructionType.LoadPi:
                    case InstructionType.LoadTau:
                    case InstructionType.LoadZero:
                        // Already initialized correctly (two Zeros)
                        goto Lookup;
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
                        tableArgs = (stack.Pop(), stack.Pop());
                        goto Lookup;
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
                        tableArgs = (stack.Pop(), Number.Zero);
                        goto Lookup;
                    Lookup:
                        stack.Push(new(
                            Instructions.Table[tableIndex]
                            (tableArgs.Item1.Value, tableArgs.Item2.Value)));
                        break;
                    case InstructionType.Count:
                    default:
                        InterpreterError("Not an instruction!");
                        break;
                }
            }
        }

        private void Restore()
        {
            Volatile.RestoreTo(Stable);
        }

        private void InitStable()
        {
            Stable.RestoreTo(Volatile);
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
