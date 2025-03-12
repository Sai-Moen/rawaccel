using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Text;
using userspace_backend.ScriptingLanguage;
using userspace_backend.ScriptingLanguage.Compiler;

namespace userspace_backend_tests.ScriptingLanguageTests.IntegrationTests;

[TestClass]
public class ParserTests
{
    private static ASTNode[] GetCalculationASTs(string script)
    {
        (Context _, AST ast) = Wrapper.CompileToAST(script);
        foreach (ASTNode node in ast.Declarations)
        {
            if (node.Tag != ASTTag.Callback)
                continue;

            ASTCallback callback = node.Union.astCallback;
            if ((ExtraIndexCallback)callback.Identifier.ExtraIndex == ExtraIndexCallback.Calculation)
                return callback.Code;
        }

        Debug.Fail("Unreachable: no calculation block without exception thrown?");
        return [];
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

        StringBuilder builder = new($"[] var {name} := 1; callback calculation {{ y += ");
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

        ASTNode[] code = GetCalculationASTs(builder.ToString());
        Token[] firstStatementInitializer = code[0].Union.astAssign.Initializer;

        int index = 0;
        void AssertNextToken(Token expected)
        {
            Token actual = firstStatementInitializer[index++];
            Assert.AreEqual(expected.Kind, actual.Kind);
            Assert.AreEqual(expected.ExtraIndex, actual.ExtraIndex);
        }

        for (int i = 0; i <= depth; i++)
        {
            AssertNextToken(new(TokenKind.Zero));
        }
        AssertNextToken(new(TokenKind.Impersistent));
        for (int i = 0; i <= depth; i++)
        {
            AssertNextToken(Tokens.GetReserved(Tokens.SUB));
        }
    }
}
