﻿using scripting.Lexical;

namespace scripting.Syntactical;

public static class LexicalHelpers
{
    public static Token WithType(this Token token, TokenType type) => token with { Base = token.Base with { Type = type } };

    public static bool LeftAssociative(this Token token)
    {
        // only exponentiation is right-associative at the moment
        return token.Symbol != Tokens.POW;
    }

    public static int Precedence(this Token token) => token.Symbol switch
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
}