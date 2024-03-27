using scripting.Lexical;

namespace scripting.Interpretation;

public static class LexicalHelpers
{
    // only while is a loop at the moment
    public static bool IsLoop(this Token token) => token.Symbol == Tokens.BRANCH_WHILE;
}
