using System.Collections.Generic;
using System.Diagnostics;

namespace userinterface.Models.Script.Generation
{
    /// <summary>
    /// Represents a parsed list of tokens.
    /// </summary>
    /// <param name="Tokens"></param>
    public record TokenCode(Token[] Tokens)
    {
        public TokenCode(TokenList tokens) : this(tokens.ToArray()) { }

        public TokenCode(Expression expr) : this(expr.Tokens) { }

        public int Length { get { return Tokens.Length; } }

        public Token this[int index]
        {
            get { return Tokens[index]; }
            set { Tokens[index] = value; }
        }

        public static implicit operator TokenCode(TokenList list)
        {
            return new(list);
        }

        public static implicit operator TokenCode(Expression expr)
        {
            return new(expr);
        }
    }

    /// <summary>
    /// Saves the Token of a Parameter and its (default) value.
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
    /// Collection of Parameter assignments.
    /// </summary>
    public class Parameters : List<ParameterAssignment>
    {
        public Parameters() : base(Constants.MAX_PARAMETERS) { }
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

    /// <summary>
    /// Represents a parsed expression.
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
    /// Collection of Variable assignments.
    /// </summary>
    public class Variables : List<VariableAssignment>
    {
        public Variables() : base(Constants.MAX_VARIABLES) { }
    }

    /// <summary>
    /// Collection of identifier names.
    /// </summary>
    public class Identifiers : HashSet<string>, ISet<string>
    {
        public Identifiers(int capacity) : base(capacity) { }
    }
}
