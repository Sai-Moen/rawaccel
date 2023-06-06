using System.Collections.Generic;
using System.Diagnostics;

namespace userinterface.Models.Script.Generation
{
    public abstract class ParserNode
    {
        public Token? Token { get; init; }
    }

    public class ParameterAssignment : ParserNode
    {
        public ParameterAssignment(Token token, Token value)
        {
            Token = token;
            Debug.Assert(Token.Base.Type == TokenType.Parameter);

            Debug.Assert(value.Base.Type == TokenType.Number);

            double result;
            if (double.TryParse(value.Base.Symbol, out result))
            {
                Value = result;
            }
            else
            {
                throw new ParserException(value.Line, "Cannot parse number!");
            }
        }

        public double Value { get; init; }
    }

    public class VariableAssignment : ParserNode
    {
        public VariableAssignment(Token token, Token value)
        {
            Token = token;
            Debug.Assert(Token.Base.Type == TokenType.Variable);

            TokenType valueType = value.Base.Type;
            Debug.Assert(valueType == TokenType.Number || valueType == TokenType.Parameter);

            if (valueType == TokenType.Parameter)
            {
                return; // Dynamically assign
            }

            double result;
            if (double.TryParse(value.Base.Symbol, out result))
            {
                Value = result;
            }
            else
            {
                throw new ParserException(value.Line, "Cannot parse number!");
            }
        }

        public double Value { get; init; }
    }

    public class Expression : ParserNode
    {
        public Expression(Token token, TokenList tokens)
        {
            Token = token;
            Tokens = tokens;
        }

        public TokenList Tokens { get; init; }
    }

    public class TokenStack : Stack<Token>
    {
        public TokenStack() : base() {}
    }

    public class TokenQueue : Queue<Token>
    {
        public TokenQueue() : base() {}
    }
}
