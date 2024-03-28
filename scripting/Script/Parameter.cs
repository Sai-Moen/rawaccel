using scripting.Common;
using scripting.Lexical;
using scripting.Syntactical;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

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
/// Defines the types a parameter can have.
/// </summary>
public enum ParameterType
{
    Real,
    Logical,
}

/// <summary>
/// Saves the name of a <see cref="Parameter"/> and its value.
/// Additionally, information about the bounds (Guard) of the parameter is saved.
/// </summary>
public class Parameter
{
    #region Fields

    private readonly ParameterValidation min;
    private readonly ParameterValidation max;

    #endregion

    #region Constructors

    internal Parameter(
        Token name, Token value,
        Token? minGuard, Token? minValue,
        Token? maxGuard, Token? maxValue)
    {
        Debug.Assert(name.Type == TokenType.Parameter);
        Name = name.Symbol;

        switch (value.Type)
        {
            case TokenType.Number:
                Type = ParameterType.Real;
                Value = (Number)value;
                break;
            case TokenType.Bool:
                Type = ParameterType.Logical;
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
            throw new GenerationException("Contradicting parameter bounds!", minGuard.Line);
        }
    }

    #endregion

    #region Properties

    /// <summary>
    /// Name of this Parameter (/\w+/ with underscores normalized to spaces).
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The current value of this Parameter.
    /// </summary>
    public Number Value { get; set; }

    /// <summary>
    /// The type of the parameter.
    /// </summary>
    public ParameterType Type { get; }

    #endregion

    #region Static Methods

    private static ParameterValidation GuardHelper(Token? token, Token? value, Func<Token, Guard> func)
    {
        if (token is null || value is null)
        {
            return new ParameterValidation(Guard.None);
        }

        Guard guard = func(token);
        return new ParameterValidation(guard, (Number)value);
    }

    private static Guard OnMinGuard(Token token) => token.Symbol switch
    {
        Tokens.GT => Guard.Greater,
        Tokens.GE => Guard.GreaterEq,

        _ => throw new GenerationException("Incorrect guard! (minimum)", token.Line)
    };

    private static Guard OnMaxGuard(Token token) => token.Symbol switch
    {
        Tokens.LT => Guard.Less,
        Tokens.LE => Guard.LessEq,

        _ => throw new GenerationException("Incorrect guard! (maximum)", token.Line)
    };

    #endregion

    #region Methods

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

    #endregion
}

/// <summary>
/// Collection of <see cref="Parameter"/> declarations.
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

/// <summary>
/// Read-Only representation of a <see cref="Parameter"/>.
/// </summary>
/// <param name="Name">the name</param>
/// <param name="Value">the value</param>
/// <param name="Type">the type</param>
public readonly record struct ReadOnlyParameter(string Name, Number Value, ParameterType Type)
{
    internal ReadOnlyParameter(Parameter parameter) : this(parameter.Name, parameter.Value, parameter.Type) { }
}

/// <summary>
/// Read-Only collection of <see cref="ReadOnlyParameter"/>.
/// </summary>
public class ReadOnlyParameters : ReadOnlyCollection<ReadOnlyParameter>, IList<ReadOnlyParameter>
{
    internal ReadOnlyParameters(IList<Parameter> parameters) : base(Wrap(parameters)) { }

    private static List<ReadOnlyParameter> Wrap(IList<Parameter> parameters)
    {
        List<ReadOnlyParameter> ro = new(parameters.Count);
        foreach (Parameter parameter in parameters)
        {
            ro.Add(new(parameter));
        }
        return ro;
    }
}