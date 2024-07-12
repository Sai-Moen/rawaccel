using scripting.Common;
using scripting.Interpreting;
using scripting.Lexing;
using scripting.Parsing;
using System;
using System.IO;

namespace scripting;

/// <summary>
/// Wrapper for scripting.
/// </summary>
public static class Wrapper
{
    /// <summary>
    /// Attemps to load a RawAccelScript script.
    /// </summary>
    /// <param name="script">script string to load</param>
    /// <returns>interpreter instance with the loaded script</returns>
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
    /// </summary>
    /// <param name="scriptPath">path to load from</param>
    /// <returns>interpreter instance with the loaded script</returns>
    /// <exception cref="ScriptException"/>
    public static IInterpreter LoadScriptFromFile(string scriptPath)
    {
        string script = File.ReadAllText(scriptPath);
        return LoadScript(script);
    }

    /// <summary>
    /// Utility method to generate an error message for script writers,
    /// e.g. "[Emit] Branch mismatch!" for some EmitException.
    /// </summary>
    /// <param name="e">the error</param>
    /// <returns>exception message with type prepended</returns>
    public static string GenerateErrorMessage(ScriptException e)
    {
        string name = e.GetType().Name;
        int startIndex = name.IndexOf(nameof (Exception));
        return startIndex == -1 ? e.Message : $"[{name.Remove(startIndex)}] {e.Message}";
    }
}
