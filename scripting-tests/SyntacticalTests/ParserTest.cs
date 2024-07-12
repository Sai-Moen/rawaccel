using scripting.Lexing;
using scripting.Parsing;
using System.Collections.Generic;
using System.Text;

namespace scripting_tests.ParserTests;

[TestClass]
public class ParserTest
{
    private static IList<Token> GenerateParsingResultTokens(string script)
    {
        Lexer lexer = new(script);
        LexingResult input = lexer.Tokenize();

        Parser parser = new(input);
        return parser.Parse().Callbacks[0].Code[0].Union.astAssign.Initializer;
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(5)]
    [DataRow(6)]
    [DataRow(7)]
    public void TestUnaryMinus(int depth)
    {
        const string name = "um";

        StringBuilder builder = new($"[] {name} := 1; {{ y += ");
        for (int i = 0; i < depth; i++)
        {
            builder.Append("-(");
        }
        builder.Append($"-{name}");
        for (int i = 0; i < depth; i++)
        {
            builder.Append(')');
        }
        builder.Append("; }");

        IList<Token> result = GenerateParsingResultTokens(builder.ToString());

        int index = 0;
        void AssertNextToken(Token token) => Assert.AreEqual(token, result[index++]);

        for (int i = 0; i <= depth; i++)
        {
            AssertNextToken(Tokens.GetReserved(Tokens.ZERO, 1));
        }
        AssertNextToken(new(new(TokenType.Variable, name), 1));
        for (int i = 0; i <= depth; i++)
        {
            AssertNextToken(Tokens.GetReserved(Tokens.SUB, 1));
        }
    }
}
