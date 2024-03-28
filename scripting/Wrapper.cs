using scripting.Common;
using scripting.Interpretation;
using scripting.Lexical;
using scripting.Syntactical;
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
        string script = ScriptLoader.LoadScript(scriptPath);
        return LoadScript(script);
    }
}

/// <summary>
/// Tries to load all the text in a script.
/// </summary>
public static class ScriptLoader
{
    public const int MaxScriptFileLength = 0xFFFF;

    public static string LoadScript(string scriptPath)
    {
        if (!File.Exists(scriptPath))
        {
            throw new LoaderException("File not found!");
        }
        else if (new FileInfo(scriptPath).Length > MaxScriptFileLength)
        {
            throw new LoaderException("File too big!");
        }

        try
        {
            return File.ReadAllText(scriptPath);
        }
        catch
        {
            // Could differentiate between certain exceptions,
            // keep in mind that File.Exists already catches a decent amount.
            throw new LoaderException("File not readable!");
        }
    }
}

/// <summary>
/// Exception for errors with loading scripts.
/// </summary>
public sealed class LoaderException(string message) : ScriptException(message)
{
}
