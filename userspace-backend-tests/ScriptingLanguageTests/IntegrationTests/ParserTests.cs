﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using userspace_backend.ScriptingLanguage;
using userspace_backend.ScriptingLanguage.Compiler.Parser;
using userspace_backend.ScriptingLanguage.Compiler.Tokenizer;

namespace userspace_backend_tests.ScriptingLanguageTests.IntegrationTests;

[TestClass]
public class ParserTests
{
    private static ASTNode[] GetCalculationASTs(string script)
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

        ASTNode[] code = GetCalculationASTs(builder.ToString());
        Token[] firstStatementInitializer = code[0].Union.astAssign.Initializer;

        int index = 0;
        void AssertNextToken(Token expected)
        {
            Token actual = firstStatementInitializer[index++];
            Assert.AreEqual(expected.Type, actual.Type);
            Assert.AreEqual(expected.ExtraIndex, actual.ExtraIndex);
        }

        for (int i = 0; i <= depth; i++)
        {
            AssertNextToken(Tokens.GetReserved(Tokens.ZERO));
        }
        AssertNextToken(new(TokenType.Impersistent));
        for (int i = 0; i <= depth; i++)
        {
            AssertNextToken(Tokens.GetReserved(Tokens.SUB));
        }
    }
}