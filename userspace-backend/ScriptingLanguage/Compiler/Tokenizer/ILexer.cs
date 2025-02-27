namespace userspace_backend.ScriptingLanguage.Compiler.Tokenizer;

/// <summary>
/// Defines the API of a RawAccelScript lexer.
/// </summary>
public interface ILexer
{
    /// <summary>
    /// Advances the lexer by a token.
    /// </summary>
    /// <returns>
    /// The next token, or Tokens.DUMMY on out-of-bounds.
    /// It is up to the parser to decide if a particular token was reached at the wrong time.
    /// </returns>
    Token Advance();

    /// <summary>
    /// Sets the state of the lexer to be back at the start of the script.
    /// </summary>
    void Reset();
}

/// <summary>
/// Exception for tokenizing-specific errors.
/// </summary>
public sealed class LexerException(string message, Token suspect)
    : CompilationException(message, suspect)
{ }
