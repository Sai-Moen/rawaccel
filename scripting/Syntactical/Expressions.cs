using scripting.Common;
using scripting.Lexical;

namespace scripting.Syntactical;

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
    #region Constructors

    public ParameterAssignment(
        Token name, Token value,
        Token? minGuard, Token? min,
        Token? maxGuard, Token? max)
    {
        Debug.Assert(name.Base.Type == TokenType.Parameter);
        Name = name.Base.Symbol;

        IsNumber = value.Base.Type == TokenType.Number;
        if (IsNumber)
        {
            Value = (Number)value;
        }
        else
        {
            Debug.Assert(value.Base.Type == TokenType.Boolean);
            Value = value.FromBoolean();
        }

        Min = GuardHelper(minGuard, min, OnMinGuard);
        Max = GuardHelper(maxGuard, max, OnMaxGuard);
    }

    #endregion

    #region Static Methods

    private static ParameterValidation GuardHelper(Token? token, Token? value, Func<string, Guard> func)
    {
        if (token is null || value is null)
        {
            return new ParameterValidation(Guard.None);
        }

        Guard guard = func(token.Base.Symbol);
        return new ParameterValidation(guard, (Number)value);
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

    #endregion

    #region Properties

    public string Name { get; }

    public bool IsNumber { get; }
    public Number Value { get; }

    public ParameterValidation Min { get; }
    public ParameterValidation Max { get; }

    #endregion
}

/// <summary>
/// Collection of Parameter assignments.
/// </summary>
public class Parameters : List<ParameterAssignment>, IList<ParameterAssignment>
{
    public Parameters() : base(Constants.MAX_PARAMETERS) { }
}

/// <summary>
/// Saves the Token of a Variable and its Expression.
/// </summary>
public record VariableAssignment(string Name, IList<Token> Expr);

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
