using System;
using System.IO;
using userinterface.Models.Script.Generation;

namespace userinterface.Models.Script;

/// <summary>
/// Combines all components of Script.
/// </summary>
public class Script
{
    private Interpreter? interpreter;

    /// <summary>
    /// Returns Interpreter instance if one is loaded,
    /// otherwise throws <see cref="LoaderException"/>.
    /// </summary>
    public Interpreter Interpreter => interpreter ?? throw new LoaderException("No script loaded!");

    /// <summary>
    /// Attempts to load a RawAccelScript script from <paramref name="scriptPath"/>.
    /// Throws <see cref="ScriptException"/> on bad script input.
    /// </summary>
    public void LoadScript(string scriptPath)
    {
        string script = ScriptLoader.LoadScript(scriptPath);
        Tokenizer tokenizer = new(script);
        Parser parser = new(tokenizer.TokenList);
        interpreter = new(parser.Parameters, parser.Variables, parser.TokenCode);
    }
}

/// <summary>
/// Exception to derive from when doing anything inside the Script namespace.
/// </summary>
public abstract class ScriptException : Exception
{
    public ScriptException(string message) : base(message) { }
}

/// <summary>
/// Tries to load all the text in a script.
/// </summary>
public static class ScriptLoader
{
    public const long MaxScriptFileLength = 0xFFFF;

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
public sealed class LoaderException : ScriptException
{
    public LoaderException(string message) : base(message) { }
}
