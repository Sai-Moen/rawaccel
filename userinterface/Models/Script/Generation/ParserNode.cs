using System.Collections.Generic;
using System.Diagnostics;

namespace userinterface.Models.Script.Generation
{
    public record Expression(Token[] Tokens)
    {
        public Expression(TokenList tokens) : this(tokens.ToArray()) { }
    }

    public abstract class ParserNode
    {
        public abstract Token Token { get; }

        public abstract bool IsExpression { get; }

        public abstract double? Value { get; init; }

        public abstract Expression? Expr { get; init; }
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
                Value = result;
            }
            else
            {
                throw new ParserException(value.Line, "Cannot parse number!");
            }
        }

        public override Token Token { get; }

        public override bool IsExpression => false;

        public override double? Value { get; init; }

        public override Expression? Expr { get => null; init { } }
    }

    public class VariableAssignment : ParserNode
    {
        private readonly double? _value;

        private readonly Expression? _expression;

        public VariableAssignment(Token token, Token[] value)
        {
            Token = token;
            Debug.Assert(Token.Base.Type == TokenType.Variable);

            Token number = value[0];
            TokenType valueType = number.Base.Type;

            IsExpression = valueType == TokenType.Parameter;
            if (IsExpression)
            {
                Expr = new(value);
            }
            else if (double.TryParse(number.Base.Symbol, out double result))
            {
                Value = result;
            }
            else
            {
                throw new ParserException(number.Line, "Cannot parse number!");
            }
        }

        public override Token Token { get; }

        public override bool IsExpression { get; }

        public override double? Value
        {
            get => _value ?? throw new ParserException("Unchecked use of Value!");
            init => _value = value;
        }

        public override Expression? Expr
        {
            get => _expression ?? throw new ParserException("Unchecked use of Parameter!");
            init => _expression = value;
        }
    }

    public class TokenStack : Stack<Token>
    {
        public TokenStack() : base() { }
    }

    public class TokenQueue : Queue<Token>
    {
        public TokenQueue() : base() { }
    }
}
