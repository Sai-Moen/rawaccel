﻿using scripting.Common;

namespace scripting.Lexical;

/// <summary>
/// Defines the API of a RawAccelScript lexer.
/// </summary>
public interface ILexer
{
    /// <summary>
    /// Makes the lexer begin tokenizing (single-use only).
    /// </summary>
    /// <returns>result of tokenizing</returns>
    /// <exception cref="LexerException"/>
    LexingResult Tokenize();
}

/// <summary>
/// The result of succesfully performing lexical analysis on an RAS script.
/// </summary>
/// <param name="Comments">the comments of the script</param>
/// <param name="Tokens">the tokens after the comments</param>
public record LexingResult(string Comments, IList<Token> Tokens);

/// <summary>
/// Exception for tokenizing-specific errors.
/// </summary>
public sealed class LexerException : GenerationException
{
    public LexerException(string message, uint line) : base(message, line) { }
}
