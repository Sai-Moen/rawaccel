using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace userinterface.Models.Script.Generation;

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
/// The operation to validate a Number against a certain value with.
/// </summary>
public enum Guard
{
    None,
    Less,
    LessEq,
    Greater,
    GreaterEq,
}

/// <summary>
/// Determines how to validate a parameter.
/// </summary>
public record ParameterValidation(Guard GuardType, Number GuardValue = default)
{
    public bool IsValid(Number value) => GuardType switch
    {
        Guard.None => true,
        Guard.Less => value < GuardValue,
        Guard.LessEq => value <= GuardValue,
        Guard.Greater => value > GuardValue,
        Guard.GreaterEq => value >= GuardValue,

        _ => throw new ParserException("Invalid Guard value!")
    };
}

/// <summary>
/// Saves the Token of a Parameter and its (default) value.
/// Additionally, information about the bounds (Guard) of the parameter can be saved.
/// </summary>
public class ParameterAssignment
{
    public ParameterAssignment(
        Token token, Token value,
        Token? minGuard, Token? min,
        Token? maxGuard, Token? max)
    {
        Debug.Assert(token.Base.Type == TokenType.Parameter);
        Name = token.Base.Symbol;

        Debug.Assert(value.Base.Type == TokenType.Number);
        Value = (Number)value;

        Min = GuardHelper(minGuard, min, OnMinGuard);
        Max = GuardHelper(maxGuard, max, OnMaxGuard);
    }

    public string Name { get; }

    public Number Value { get; }

    public ParameterValidation Min { get; }

    public ParameterValidation Max { get; }

    private static ParameterValidation GuardHelper(Token? token, Token? value, Func<string, Guard> func)
    {
        if (token is not null && value is not null)
        {
            Guard guard = func(token.Base.Symbol);
            return new ParameterValidation(guard, (Number)value);
        }
        return new ParameterValidation(Guard.None);
    }

    private static Guard OnMinGuard(string symbol) => symbol switch
    {
        Tokens.GT => Guard.Greater,
        Tokens.GE => Guard.GreaterEq,

        _ => throw new ParserException("Incorrect guard! (minimum)")
    };

    private static Guard OnMaxGuard(string symbol) => symbol switch
    {
        Tokens.LT => Guard.Less,
        Tokens.LE => Guard.LessEq,

        _ => throw new ParserException("Incorrect guard! (maximum)")
    };
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
