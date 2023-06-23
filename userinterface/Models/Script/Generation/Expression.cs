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
    /// Saves the Token of a Parameter and its value.
    /// </summary>
    public record ParameterAssignment(string Name, Number Value)
    {
        public ParameterAssignment(Token token, Token value)
            : this(token.Base.Symbol, (Number)value)
        {
            Debug.Assert(token.Base.Type == TokenType.Parameter);
            Debug.Assert(value.Base.Type == TokenType.Number);
        }
    }

    /// <summary>
    /// Saves the Token of a Variable and its Expression.
    /// </summary>
    public record VariableAssignment(string Name, Expression Expr)
    {
        public VariableAssignment(Token token, TokenList expr)
            : this(token.Base.Symbol, expr)
        {
            Debug.Assert(token.Base.Type == TokenType.Variable);
        }
    }

    public class Parameters : List<ParameterAssignment>
    {
        public Parameters() : base(Constants.MAX_PARAMETERS) { }
    }

    public class Variables : List<VariableAssignment>
    {
        public Variables() : base(Constants.MAX_VARIABLES) { }
    }

    public class Identifiers : List<string>, IList<string>
    {
        public Identifiers(int capacity) : base(capacity) { }
    }

    public class TokenStack : Stack<Token>
    { }

    public class TokenQueue : Queue<Token>
    { }
}
