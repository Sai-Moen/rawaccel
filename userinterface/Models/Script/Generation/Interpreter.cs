using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace userinterface.Models.Script.Generation
{
    public class Interpreter
    {
        #region Fields

        private double x = 0;

        private double y = 1;

        private readonly MemoryMap Addresses = new();

        private readonly MemoryHeap Stable = new();

        private readonly MemoryHeap Volatile = new();

        private readonly Program MainProgram;

        private readonly Program[] Startup;

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

            Settings = Defaults;

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

                Startup[i] = new(expr, Addresses, address);
            }

            MainProgram = new(new(code), Addresses, null);
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// The parameters and their default values from the script.
        /// </summary>
        public ParameterPairs Defaults { get; } = new();

        /// <summary>
        /// The current values of all parameters.
        /// </summary>
        public ParameterPairs Settings { get; set; } = new();

        #endregion Properties

        #region Methods

        public double Calculate(double x)
        {
            this.x = x;

            return Exec(MainProgram);
        }

        public void Init()
        {
            y = 1;

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
             *   so that's why Exec has access to all instructions.
             */
            Parallel.ForEach(Startup, (p) =>
            {
                MemoryAddress owner = p.Owner ?? throw new InterpreterException(
                    "Startup thread should have an owner!");
                Stable[owner] = Exec(p);
            });

            // Load everything to Volatile
            Restore();
        }

        private double Exec(Program program)
        {
            // Since Init uses Parallel.ForEach, don't write to instance variables!
            // This includes pulling out local variables here and modifying them...
            Stack<double> stack = new();

            for (int i = 0; i < program.Instructions.Count; i++)
            {
                Instruction instruction = program.Instructions[i];
                InstructionType type = (InstructionType)instruction[0];
                switch (type)
                {
                    case InstructionType.End:
                        if (i != program.Instructions.Count - 1)
                        {
                            InterpreterError("Unexpected program end!");
                        }

                        return y;
                    case InstructionType.Load:
                        MemoryAddress loadAddress = new(instruction);
                        stack.Push(Volatile[loadAddress]);
                        i += MemoryAddress.Size;
                        break;
                    case InstructionType.Store:
                        MemoryAddress storeAddress = new(instruction);
                        Volatile[storeAddress] = stack.Pop();
                        i += MemoryAddress.Size;
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
                        Number number = new(instruction);
                        stack.Push(number.Value);
                        i += Number.Size;
                        break;
                    case InstructionType.Jmp:
                        CodeAddress jmpAddress = new(instruction);
                        i = jmpAddress.Address;
                        break;
                    case InstructionType.Jz:
                        CodeAddress jzAddress = new(instruction);
                        i = stack.Pop() == 0 ?
                            jzAddress.Address : i + CodeAddress.Size;
                        break;
                    case InstructionType.Count:
                        InterpreterError("Not an instruction!");
                        break;
                    default:
                        try
                        {
                            stack.Push(
                                Instructions.Table[type.ToByte()](
                                    stack.Pop(), stack.Pop()));
                        }
                        catch
                        {
                            InterpreterError("Lookup failed!");
                        }
                        
                        break;
                }
            }

            return 0;
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
