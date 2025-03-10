using System;
using System.IO;
using userspace_backend.ScriptingLanguage.Compiler;

namespace userspace_backend.ScriptingLanguage;

public interface IScriptFile
{
    public string Description { get; }

    public ReadOnlyParameters Defaults { get; }

    public Parameters Settings { get; }

    /// <summary>
    /// Runs the compiled script, mapping the appropriate distribution onto the calculation.
    /// </summary>
    /// <param name="desiredMaxSpeed"></param>
    /// <returns></returns>
    double[] RunScript(double desiredMaxSpeed);

    double[] Calculate(double[] xs);
}

/// <summary>
/// Root ScriptingLanguage exception.
/// </summary>
public class ScriptException : Exception
{
    public ScriptException(string message)
        : base(message)
    { }

    public ScriptException(string message, Exception innerException)
        : base(message, innerException)
    { }
}

/// <summary>
/// Wrapper for scripting.
/// </summary>
public static class Wrapper
{
    public static IScriptFile LoadScriptFromFile(string scriptPath)
    {
        string script;
        try
        {
            script = File.ReadAllText(scriptPath);
        }
        catch (SystemException e)
        {
            throw new ScriptException("An error occurred while trying to read the file!", e);
        }

        return LoadScript(script);
    }

    public static IScriptFile LoadScript(string script)
    {
        (Context context, AST ast) = CompileToAST(script);
        return new ScriptFile(context, ast);
    }

    public static (Context, AST) CompileToAST(string script)
    {
        Context context = new(script);
        Parser parser = new(context, new Lexer(context));
        return (context, parser.Parse());
    }

    public static (Context, Interpreter) CompileToInterpreter(string script)
    {
        (Context context, AST ast) = CompileToAST(script);
        Emitter emitter = new(context);
        Interpreter interpreter = new(ast, emitter);
        return (context, interpreter);
    }
}
