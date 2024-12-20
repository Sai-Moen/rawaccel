using System.Collections.Generic;

namespace userspace_backend.ScriptingLanguage.Compiler.Tokenizer;

/// <summary>
/// Defines the API of a RawAccelScript lexer.
/// </summary>
public interface ILexer
{
    /// <summary>
    /// Makes the lexer begin tokenizing (single-use only).
    /// </summary>
    /// <returns>Result of tokenizing.</returns>
    /// <exception cref="LexerException"/>
    LexingResult Tokenize();
}

/// <summary>
/// The result of tokenizing a script.
/// </summary>
/// <param name="Context">The compiler's context.</param>
/// <param name="Description">The description of the script.</param>
/// <param name="Tokens">The tokens after the description.</param>
public record LexingResult(CompilerContext Context, string Description, IList<Token> Tokens);

/// <summary>
/// Exception for tokenizing-specific errors.
/// </summary>
public sealed class LexerException(string message, Token suspect)
    : CompilationException(message, suspect)
{ }
