using scripting.Lexing;
using System.Diagnostics;

namespace scripting.Parsing;

internal record Operator(Token Token, int Precedence)
{
    internal TokenType Type => Token.Type;

    internal bool HasHigherPrecedence(Operator other, bool left)
        => Type.HasPrecedence() &&
            (Precedence > other.Precedence || left && Precedence == other.Precedence);
}

/// <summary>
/// Provides helper/extension methods for dealing with tokens when parsing.
/// </summary>
public static class LexicalHelpers
{
    /// <summary>
    /// Looks up if the given token type can be considered to have precedence.
    /// </summary>
    /// <param name="type">Type of the token</param>
    /// <returns>Whether the token type has precedence</returns>
    public static bool HasPrecedence(this TokenType type) => type switch
    {
        TokenType.Arithmetic => true,
        TokenType.Comparison => true,

        _ => false,
    };

    /// <summary>
    /// Returns a copy of a token with the given type.
    /// </summary>
    /// <param name="token">Token to copy</param>
    /// <param name="type">Type to change to</param>
    /// <returns></returns>
    public static Token WithType(this Token token, TokenType type) => token with { Base = token.Base with { Type = type } };

    /// <summary>
    /// Maps a token to the type that the identifier will have.
    /// The type in this case refers to mutability and persistence.
    /// </summary>
    /// <param name="token">The declarer</param>
    /// <returns>Type of the identifier</returns>
    public static TokenType MapDeclarer(this Token token) => token.Type switch
    {
        TokenType.Const => TokenType.Immutable,
        TokenType.Let => TokenType.Persistent,
        TokenType.Var => TokenType.Impersistent,

        _ => TokenType.Undefined
    };

    /// <summary>
    /// Looks up if the given token is left-associative.
    /// </summary>
    /// <param name="token">The token</param>
    /// <returns>Whether the token is left-associative</returns>
    public static bool LeftAssociative(this Token token)
    {
        Debug.Assert(token.Type.HasPrecedence());
        return token.Symbol != Tokens.POW;
    }

    /// <summary>
    /// Gets the precedence level of the given token.
    /// The unary flag should be true if this is a unary operation with an operator that also has non-unary uses.
    /// </summary>
    /// <param name="token">The token</param>
    /// <param name="unary">Whether this is the unary form of an operator</param>
    /// <returns>The precedence level of the token</returns>
    /// <exception cref="ParserException"/>
    public static int Precedence(this Token token, bool unary = false)
    {
        int prec = token.Symbol switch
        {
            Tokens.OR => 0,
            Tokens.AND => 1,

            Tokens.EQ => 2,
            Tokens.NE => 2,

            Tokens.LT => 3,
            Tokens.GT => 3,
            Tokens.LE => 3,
            Tokens.GE => 3,

            Tokens.ADD => 4,
            Tokens.SUB => 4,

            Tokens.MUL => 5,
            Tokens.DIV => 5,
            Tokens.MOD => 5,

            Tokens.POW => 6,

            // this one is also unary, but the unary system is kind of a hack for when an operator has a unary and binary form
            Tokens.NOT => 7,

            _ => throw new ParserException("Unexpected Precedence call!", token.Line)
        };

        if (unary)
        {
            const int unaryPrecedenceAdd = 8;
            Debug.Assert(prec < unaryPrecedenceAdd, "The maximum precedence level here should be lower than what we add...");
            prec += unaryPrecedenceAdd;
        }

        return prec;
    }
}
