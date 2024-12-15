using System.Diagnostics;
using userspace_backend.ScriptingLanguage.Lexing;

namespace userspace_backend.ScriptingLanguage.Parsing;

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
    /// <param name="type">Type of the token.</param>
    /// <returns>Whether the token type has precedence.</returns>
    public static bool HasPrecedence(this TokenType type) => type switch
    {
        TokenType.Arithmetic => true,
        TokenType.Comparison => true,

        _ => false,
    };

    /// <summary>
    /// Maps a token to the type that the identifier will have.
    /// The type in this case refers to mutability and persistence.
    /// </summary>
    /// <param name="token">The declarer.</param>
    /// <returns>Type of the identifier.</returns>
    public static TokenType MapDeclarer(this Token token) => token.Type switch
    {
        TokenType.Const => TokenType.Immutable,
        TokenType.Let => TokenType.Persistent,
        TokenType.Var => TokenType.Impersistent,
        TokenType.Fn => TokenType.Function,

        _ => TokenType.None
    };

    /// <summary>
    /// Looks up if the given token is left-associative.
    /// </summary>
    /// <param name="token">The token.</param>
    /// <returns>Whether the token is left-associative.</returns>
    public static bool LeftAssociative(this Token token)
    {
        Debug.Assert(token.Type.HasPrecedence());
        return token.Type == TokenType.Arithmetic && (ExtraIndexArithmetic)token.ExtraIndex != ExtraIndexArithmetic.Pow;
    }

    /// <summary>
    /// Gets the precedence level of the given token.
    /// The unary flag should be true if this is a unary operation with an operator that also has non-unary uses.
    /// </summary>
    /// <param name="token">The token.</param>
    /// <param name="unary">Whether this is the unary form of an operator.</param>
    /// <returns>The precedence level of the token.</returns>
    /// <exception cref="ParserException"/>
    public static int Precedence(this Token token, bool unary = false)
    {
        int prec = token.Type switch
        {
            TokenType.Comparison => (ExtraIndexComparison)token.ExtraIndex switch
            {
                ExtraIndexComparison.Or => 0,

                ExtraIndexComparison.And => 1,

                ExtraIndexComparison.Equal    => 2,
                ExtraIndexComparison.NotEqual => 2,

                ExtraIndexComparison.LessThan           => 3,
                ExtraIndexComparison.GreaterThan        => 3,
                ExtraIndexComparison.LessThanOrEqual    => 3,
                ExtraIndexComparison.GreaterThanOrEqual => 3,

                // this one is also unary, but the unary system is kind of a hack for when an operator has a unary and binary form
                ExtraIndexComparison.Not => 7,

                _ => throw new ParserException($"Unknown ExtraIndexComparison value: {token.ExtraIndex}", token)
            },

            TokenType.Arithmetic => (ExtraIndexArithmetic)token.ExtraIndex switch
            {
                ExtraIndexArithmetic.Add => 4,
                ExtraIndexArithmetic.Sub => 4,

                ExtraIndexArithmetic.Mul => 5,
                ExtraIndexArithmetic.Div => 5,
                ExtraIndexArithmetic.Mod => 5,

                ExtraIndexArithmetic.Pow => 6,

                _ => throw new ParserException($"Unknown ExtraIndexArithmetic value: {token.ExtraIndex}", token)
            },

            _ => throw new ParserException($"Unexpected TokenType when determining precedence: {token.Type}", token)
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
