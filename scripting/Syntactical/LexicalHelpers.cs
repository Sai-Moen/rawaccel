using scripting.Lexical;
using scripting.Script;

namespace scripting.Syntactical;

public static class LexicalHelpers
{
    public static Token? NullifyUndefined(this Token token)
    {
        return token.Base.Type == TokenType.Undefined ? null : token;
    }

    public static Number FromBoolean(this Token token) => token.Base.Symbol switch
    {
        Tokens.FALSE => Number.FALSE,
        Tokens.TRUE => Number.TRUE,

        _ => throw new ParserException("Invalid Boolean Symbol!", token.Line)
    };

    public static bool IsGuardMinimum(this Token token) => token.Base.Symbol switch
    {
        Tokens.GT or Tokens.GE => true,
        _ => false,
    };

    public static bool IsGuardMaximum(this Token token) => token.Base.Symbol switch
    {
        Tokens.LT or Tokens.LE => true,
        _ => false,
    };

    public static bool LeftAssociative(this Token token)
    {
        // only exponentiation is right-associative at the moment
        return token.Base.Symbol != Tokens.POW;
    }

    public static int Precedence(this Token token) => token.Base.Symbol switch
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
