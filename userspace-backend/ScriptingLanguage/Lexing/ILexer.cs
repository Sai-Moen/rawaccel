using System.Collections.Generic;

namespace userspace_backend.ScriptingLanguage.Lexing;

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
/// <param name="Description">The description of the script.</param>
/// <param name="Tokens">The tokens after the description.</param>
/// <param name="SymbolSideTable">The side table that contains the strings that Tokens may refer to with a SymbolIndex.</param>
public record LexingResult(string Description, IList<Token> Tokens, IList<string> SymbolSideTable);

/// <summary>
/// Exception for tokenizing-specific errors.
/// </summary>
public sealed class LexerException(string message, Token suspect)
    : GenerationException(message, suspect)
{ }
