using scripting.Generating;
using scripting.Interpreting;
using scripting.Lexing;
using scripting.Parsing;
using scripting.Script;
using System;
using System.Collections.Generic;
using System.IO;

namespace scripting;

/// <summary>
/// Wrapper for scripting.
/// </summary>
public static class Wrapper
{
    /// <summary>
    /// Attempts to load a RawAccelScript script from <paramref name="scriptPath"/>.
    /// Any exceptions while reading the file will be rethrown inside of a <see cref="ScriptException"/>.
    /// </summary>
    /// <param name="scriptPath">Path to load from</param>
    /// <returns>Interpreter instance with the loaded script</returns>
    /// <exception cref="ScriptException"/>
    public static IInterpreter LoadScriptFromFile(string scriptPath)
    {
        string script;
        try
        {
            script = File.ReadAllText(scriptPath);
        }
        catch (Exception e)
        {
            throw new ScriptException("An error occurred while trying to read the file!", e);
        }

        return LoadScript(script);
    }

    /// <summary>
    /// Attemps to load a RawAccelScript script.
    /// </summary>
    /// <param name="script">Script string to load</param>
    /// <returns>Interpreter instance with the loaded script</returns>
    /// <exception cref="ScriptException"/>
    public static IInterpreter LoadScript(string script)
    {
        return CompileToInterpreter(script);
    }

    /// <summary>
    /// Compiles a script only up to and including the Lexer phase.
    /// </summary>
    /// <param name="script">Script to compile</param>
    /// <returns>Result of lexing</returns>
    public static LexingResult CompileToLexingResult(string script)
    {
        Lexer lexer = new(script);
        return lexer.Tokenize();
    }

    /// <summary>
    /// Compiles a script only up to and including the Parser phase.
    /// </summary>
    /// <param name="script">Script to compile</param>
    /// <returns>Result of parsing</returns>
    public static ParsingResult CompileToParsingResult(string script)
    {
        Parser parser = new(CompileToLexingResult(script));
        return parser.Parse();
    }

    /// <summary>
    /// Compiles a script only up to and including the Parser phase,
    /// but also emits all callbacks present in the script to Programs.
    /// </summary>
    /// <param name="script">Script to compile</param>
    /// <returns>All callbacks as an array of <see cref="Program"/>s</returns>
    public static Program[] CompileToCallbackPrograms(string script)
    {
        ParsingResult result = CompileToParsingResult(script);

        MemoryMap addresses = [];
        int numPersistent = 0;
        int numImpersistent = 0;

        Parameters parameters = result.Parameters;
        foreach (Parameter parameter in parameters)
        {
            addresses.Add(parameter.Name, (MemoryAddress)numPersistent++);
        }

        IBlock variables = result.Declarations;
        for (int i = 0; i < variables.Count; i++)
        {
            Token identifier = variables[i].Assign!.Identifier; // TODO fix this when functions are implemented
            MemoryAddress address = identifier.Type switch
                {
                    TokenType.Immutable or TokenType.Persistent => (MemoryAddress)numPersistent++,
                    TokenType.Impersistent                      => (MemoryAddress)numImpersistent++,

                    _ => throw new ScriptException("Variable is not of the correct type here...")
                };
            addresses.Add(identifier.Symbol, address);
        }

        Emitter emitter = new(addresses);

        IList<ParsedCallback> callbacks = result.Callbacks;
        Program[] programs = new Program[callbacks.Count];
        for (int i = 0; i < callbacks.Count; i++)
        {
            IBlock code = callbacks[i].Code;
            programs[i] = emitter.Emit(code);
        }

        return programs;
    }

    /// <summary>
    /// Compiles all the way, resulting in a (concrete) Interpreter instance.
    /// Only recommended to use for testing.
    /// </summary>
    /// <param name="script">Script to compile</param>
    /// <returns>Concrete Interpreter instance</returns>
    public static Interpreter CompileToInterpreter(string script)
    {
        Interpreter interpreter = new(CompileToParsingResult(script));
        return interpreter;
    }

    /// <summary>
    /// Basic execution of a script.
    /// If the script does not have a distribution callback, we use a default distribution.
    /// If we use a default distribution, then the desired speed is an x-value that we want to approach/exceed.
    /// </summary>
    /// <param name="interpreter">Interpreter instance with the script loaded</param>
    /// <param name="desiredSpeed">Desired speed (counts/ms)</param>
    /// <returns>Calculated y-values, from distribution (x-values)</returns>
    public static double[] RunScriptBasic(IInterpreter interpreter, double desiredSpeed)
    {
        Callbacks callbacks = interpreter.Callbacks;

        double[] xs;
        if (callbacks.HasDistribution)
        {
            xs = callbacks.Distribute();
        }
        else
        {
            xs = DefaultDistribution(desiredSpeed);
        }

        return callbacks.Calculate(xs);
    }

    private static double[] DefaultDistribution(double desiredSpeed)
    {
        const int cap = Constants.LUT_POINTS_CAPACITY;

        // geometric progression or floating point magic might be preferred over this arithmetic progression
        double step = desiredSpeed / cap;

        double[] xs = new double[cap];
        for (uint i = 0; i < cap; i++)
        {
            xs[i] = i * step;
        }
        return xs;
    }
}

/// <summary>
/// Exception to derive from when doing anything inside the scripting namespace.
/// </summary>
public class ScriptException : Exception
{
    public ScriptException(string message) : base(message)
    {}

    public ScriptException(string message, Exception innerException) : base(message, innerException)
    {}
}

/// <summary>
/// Base Exception used for errors in all stages of code generation.
/// </summary>
public class GenerationException : ScriptException
{
    public GenerationException(string message) : base(message)
    {}

    public GenerationException(string message, uint line) : base($"Line {line}: {message}")
    {}
}
