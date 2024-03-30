using scripting.Common;
using scripting.Interpretation;
using scripting.Lexical;
using scripting.Syntactical;
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
        string script = ScriptLoader.ReadScript(scriptPath);
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

/// <summary>
/// Tries to load all the text in a script.
/// </summary>
public static class ScriptLoader
{
    public const int MAX_SCRIPT_LEN = 0xFFFF;

    /// <summary>
    /// Reads a script from the given path.
    /// </summary>
    /// <param name="scriptPath">the path</param>
    /// <returns>all text from the path</returns>
    /// <exception cref="LoaderException"/>
    public static string ReadScript(string scriptPath)
    {
        FileInfo info = new(scriptPath);
        if (!info.Exists)
        {
            throw new LoaderException("File not found!");
        }
        else if (info.Length > MAX_SCRIPT_LEN)
        {
            throw new LoaderException("File too long!");
        }

        try
        {
            return File.ReadAllText(scriptPath);
        }
        catch
        {
            // Could differentiate between certain exceptions,
            // keep in mind that File.Exists already catches a decent amount.
            throw new LoaderException("Cannot read script!");
        }
    }
}

/// <summary>
/// Exception for errors with loading scripts.
/// </summary>
public sealed class LoaderException(string message) : ScriptException(message)
{
}
