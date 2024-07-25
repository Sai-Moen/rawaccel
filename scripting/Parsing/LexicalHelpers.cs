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

public static class LexicalHelpers
{
    public static bool HasPrecedence(this TokenType type) => type switch
    {
        TokenType.Arithmetic => true,
        TokenType.Comparison => true,

        _ => false,
    };

    public static Token WithType(this Token token, TokenType type) => token with { Base = token.Base with { Type = type } };

    public static bool LeftAssociative(this Token token)
    {
        // only exponentiation is right-associative at the moment
        return token.Symbol != Tokens.POW;
    }

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

            Tokens.NOT => 7,

            _ => throw new ParserException("Unexpected Precedence call!", token.Line)
        };

        if (unary)
        {
            Debug.Assert(prec < 8);
            prec += 8;
        }

        return prec;
    }
}
