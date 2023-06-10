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

        public abstract double? Value { get; }

        public abstract Expression? Expr { get; }
    }

    public class ParameterAssignment : ParserNode
    {
        public ParameterAssignment(Token token, Token value)
        {
            Debug.Assert(token.Base.Type == TokenType.Parameter);
            Token = token;

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

        public override double? Value { get; }

        public override Expression? Expr => null;
    }

    public class VariableAssignment : ParserNode
    {
        public VariableAssignment(Token token, TokenList expr)
        {
            Debug.Assert(token.Base.Type == TokenType.Variable);
            Token = token;

            Debug.Assert(expr.Count != 0);
            Expr = new(expr);
        }

        public override Token Token { get; }

        public override double? Value => null;

        public override Expression? Expr { get; }
    }

    public class TokenStack : Stack<Token>
    { }

    public class TokenQueue : Queue<Token>
    { }
}
