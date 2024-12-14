using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Text;
using userspace_backend.ScriptingLanguage;
using userspace_backend.ScriptingLanguage.Lexing;
using userspace_backend.ScriptingLanguage.Parsing;

namespace userspace_backend_tests.ScriptingLanguageTests.IntegrationTests;

[TestClass]
public class ParserTests
{
    private static IList<IASTNode> GetCalculationASTs(string script)
    {
        return Wrapper.CompileToParsingResult(script).Callbacks[0].Code;
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

        StringBuilder builder = new($"[] var {name} := 1; {{ y += ");
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

        IList<IASTNode> code = GetCalculationASTs(builder.ToString());
        IList<Token> firstStatementInitializer = code[0].Assign!.Initializer;

        int index = 0;
        void AssertNextToken(Token token) => Assert.AreEqual(token, firstStatementInitializer[index++]);

        for (int i = 0; i <= depth; i++)
        {
            AssertNextToken(Tokens.GetReserved(Tokens.ZERO, 1));
        }
        AssertNextToken(new(new(TokenType.Impersistent, name), 1));
        for (int i = 0; i <= depth; i++)
        {
            AssertNextToken(Tokens.GetReserved(Tokens.SUB, 1));
        }
    }
}
