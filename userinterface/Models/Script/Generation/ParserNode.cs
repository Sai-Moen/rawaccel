using System.Collections.Generic;
using System.Diagnostics;

namespace userinterface.Models.Script.Generation
{
    public abstract class ParserNode
    {
        public abstract Token Token { get; init; }
    }

    public class ParameterAssignment : ParserNode
    {
        public ParameterAssignment(Token token, Token value)
        {
            Token = token;
            Debug.Assert(Token.Base.Type == TokenType.Parameter);

            Debug.Assert(value.Base.Type == TokenType.Number);

            if (double.TryParse(value.Base.Symbol, out double result))
            {
                DefaultValue = result;
            }
            else
            {
                throw new ParserException(value.Line, "Cannot parse number!");
            }
        }

        public override Token Token { get; init; }

        public double DefaultValue { get; init; }
    }

    public class VariableAssignment : ParserNode
    {
        public VariableAssignment(Token token, Token value)
        {
            Token = token;
            Debug.Assert(Token.Base.Type == TokenType.Variable);

            TokenType valueType = value.Base.Type;
            Debug.Assert(valueType == TokenType.Number || valueType == TokenType.Parameter);

            IsBound = valueType == TokenType.Parameter;
            if (IsBound)
            {
                Param = value; // Dynamically assign
            }
            else if (double.TryParse(value.Base.Symbol, out double result))
            {
                Value = result; // Assign a value
            }
            else
            {
                throw new ParserException(value.Line, "Cannot parse number!");
            }
        }

        public override Token Token { get; init; }

        public bool IsBound { get; init; }

        public Token? Param { get; init; }

        public double? Value { get; init; }
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
