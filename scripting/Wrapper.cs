using scripting.Common;
using scripting.Interpreting;
using scripting.Lexing;
using scripting.Parsing;
using scripting.Script;
using System;
using System.IO;

namespace scripting;

/// <summary>
/// Wrapper for scripting.
/// </summary>
public static class Wrapper
{
    /// <summary>
    /// Utility method to generate an error message to display to script writers,
    /// e.g. "[Emit] Branch mismatch!" for some EmitException.
    /// </summary>
    /// <param name="e">The error</param>
    /// <returns>Exception message with type prepended</returns>
    public static string GenerateErrorMessage(ScriptException e)
    {
        string name = e.GetType().Name;
        int startIndex = name.IndexOf(nameof (Exception));
        return startIndex == -1 ? e.Message : $"[{name.Remove(startIndex)}] {e.Message}";
    }

    /// <summary>
    /// Attemps to load a RawAccelScript script.
    /// </summary>
    /// <param name="script">Script string to load</param>
    /// <returns>Interpreter instance with the loaded script</returns>
    /// <exception cref="ScriptException"/>
    public static IInterpreter LoadScript(string script)
    {
        Lexer lexer = new(script);
        LexingResult lexicalAnalysis = lexer.Tokenize();

        Parser parser = new(lexicalAnalysis);
        ParsingResult syntacticAnalysis = parser.Parse();

        return new Interpreter(syntacticAnalysis);
    }

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
