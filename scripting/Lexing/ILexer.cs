namespace scripting.Lexing;

/// <summary>
/// Defines the API of a RawAccelScript lexer.
/// </summary>
public interface ILexer
{
    /// <summary>
    /// Makes the lexer begin tokenizing (single-use only).
    /// </summary>
    /// <returns>Result of tokenizing</returns>
    /// <exception cref="LexerException"/>
    LexingResult Tokenize();
}

/// <summary>
/// The result of tokenizing a script.
/// </summary>
/// <param name="Description">The description of the script</param>
/// <param name="Tokens">The tokens after the description</param>
public record LexingResult(string Description, ITokenList Tokens);

/// <summary>
/// Exception for tokenizing-specific errors.
/// </summary>
public sealed class LexerException(string message, uint line) : GenerationException(message, line)
{
}
