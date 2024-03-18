using scripting.Common;
using scripting.Lexical;
using scripting.Syntactical;

namespace scripting.Script;

/// <summary>
/// The operation to validate a Number against a certain value with.
/// </summary>
enum Guard
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
record ParameterValidation(Guard GuardType, Number GuardValue = default)
{
    internal int Validate(Number value) => GuardType switch
    {
        Guard.None => true,
        Guard.Less => value < GuardValue,
        Guard.LessEq => value <= GuardValue,
        Guard.Greater => value > GuardValue,
        Guard.GreaterEq => value >= GuardValue,

        _ => throw new ScriptException("Invalid Guard value!")
    } ? 1 : 0;
}

/// <summary>
/// Saves the name of a Parameter and its value.
/// Additionally, information about the bounds (Guard) of the parameter is saved.
/// </summary>
public class Parameter
{
    private readonly ParameterValidation min;
    private readonly ParameterValidation max;

    internal Parameter(
        Token name, Token value,
        Token? minGuard, Token? minValue,
        Token? maxGuard, Token? maxValue)
    {
        Debug.Assert(name.Base.Type == TokenType.Parameter);
        Name = name.Base.Symbol;

        switch (value.Base.Type)
        {
            case TokenType.Number:
                IsNumber = true;
                Value = (Number)value;
                break;
            case TokenType.Boolean:
                IsNumber = false;
                Value = value.FromBoolean();
                break;
            default:
                Debug.Fail("Unreachable: parameter value not a number or boolean!");
                return; // unreachable
        }

        min = GuardHelper(minGuard, minValue, OnMinGuard);
        max = GuardHelper(maxGuard, maxValue, OnMaxGuard);

        if (min.GuardType == Guard.None || max.GuardType == Guard.None)
        {
            // guards can't contradict, get out the way
            return;
        }

        Debug.Assert(minGuard is not null && minValue is not null && maxGuard is not null && maxValue is not null);
        if (min.Validate(max.GuardValue) == 0 || max.Validate(min.GuardValue) == 0)
        {
            throw new ParserException("Contradicting parameter bounds!", minGuard.Line);
        }
    }

    /// <summary>
    /// Name of this Parameter (\w+ with underscores normalized to spaces).
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Whether this Parameter should be interpreted as a normal number, or as a boolean.
    /// </summary>
    public bool IsNumber { get; }

    /// <summary>
    /// The current value of this Parameter.
    /// </summary>
    public Number Value { get; set; }

    private static ParameterValidation GuardHelper(Token? token, Token? value, Func<Token, Guard> func)
    {
        if (token is null || value is null)
        {
            return new ParameterValidation(Guard.None);
        }

        Guard guard = func(token);
        return new ParameterValidation(guard, (Number)value);
    }

    private static Guard OnMinGuard(Token token) => token.Base.Symbol switch
    {
        Tokens.GT => Guard.Greater,
        Tokens.GE => Guard.GreaterEq,

        _ => throw new ParserException("Incorrect guard! (minimum)", token.Line)
    };

    private static Guard OnMaxGuard(Token token) => token.Base.Symbol switch
    {
        Tokens.LT => Guard.Less,
        Tokens.LE => Guard.LessEq,

        _ => throw new ParserException("Incorrect guard! (maximum)", token.Line)
    };

    /// <summary>
    /// Validates the given value according to the Parameter's indicated bounds.
    /// </summary>
    /// <param name="value">value to validate</param>
    /// <returns>
    /// 0 on success,
    /// less than 0 for a too low value,
    /// more than 0 for a too high value.
    /// </returns>
    public int Validate(Number value)
    {
        return max.Validate(value) - min.Validate(value);
    }

    internal Parameter Clone()
    {
        return (Parameter)MemberwiseClone();
    }
}

/// <summary>
/// Collection of Parameter assignments.
/// </summary>
public class Parameters : List<Parameter>, IList<Parameter>
{
    internal Parameters() : base(Constants.MAX_PARAMETERS) { }

    private Parameters(Parameters parameters) : base(parameters) { }

    internal Parameters Clone()
    {
        Parameters clone = new(this);
        for (int i = 0; i < clone.Count; i++)
        {
            clone[i] = clone[i].Clone();
        }
        return clone;
    }
}
