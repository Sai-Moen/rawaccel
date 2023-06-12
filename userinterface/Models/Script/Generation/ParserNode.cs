using System.Collections.Generic;
using System.Diagnostics;

namespace userinterface.Models.Script.Generation
{
    /// <summary>
    /// Represents a parsed expression in a list of tokens.
    /// </summary>
    /// <param name="Tokens"></param>
    public record Expression(Token[] Tokens)
    {
        public Expression(TokenList tokens) : this(tokens.ToArray()) { }

        public static implicit operator Expression(TokenList list)
        {
            return new(list);
        }
    }

    /// <summary>
    /// Holds parsing constants.
    /// </summary>
    public static class Parsing
    {
        // Declarations
        public const int CAPACITY = byte.MaxValue + 1;
        public const int MAX_PARAMETERS = 8;
        public const int MAX_VARIABLES = CAPACITY - MAX_PARAMETERS; // Important for addressing

        static Parsing()
        {
            Debug.Assert(CAPACITY == MAX_PARAMETERS + MAX_VARIABLES);
        }
    }

    /// <summary>
    /// Base class for assignment nodes.
    /// </summary>
    public abstract class ParserNode
    {
        public abstract Token Token { get; }

        public abstract double? Value { get; }

        public abstract Expression? Expr { get; }
    }

    /// <summary>
    /// Saves the Token of a Parameter and its value.
    /// </summary>
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

    /// <summary>
    /// Saves the Token of a Variable and its Expression.
    /// </summary>
    public class VariableAssignment : ParserNode
    {
        public VariableAssignment(Token token, TokenList expr)
        {
            Debug.Assert(token.Base.Type == TokenType.Variable);
            Token = token;

            Debug.Assert(expr.Count != 0);
            Expr = expr;
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
