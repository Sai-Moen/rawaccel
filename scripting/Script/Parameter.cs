using scripting.Common;
using scripting.Lexing;
using System.Diagnostics;

namespace scripting.Script;

internal enum Bound
{
    None,
    LowerExcl,
    LowerIncl,
    UpperExcl,
    UpperIncl,
}

internal record ParameterValidation(Bound Type = Bound.None, Number Value = default)
{
    internal bool IsActive => Type != Bound.None;

    internal int Validate(Number number) => IsValid(number) ? 1 : 0;

    internal bool Contradicts(ParameterValidation other) => other.IsActive && !IsValid(other.Value);

    internal bool IsValid(Number number) => Type switch
    {
        Bound.None => true,
        Bound.LowerExcl => number >  Value,
        Bound.LowerIncl => number >= Value,
        Bound.UpperExcl => number <  Value,
        Bound.UpperIncl => number <= Value,

        _ => throw new ScriptException("Invalid Bound value!")
    };
}

/// <summary>
/// Defines the types a parameter can have.
/// </summary>
public enum ParameterType
{
    Real,
    Integer, // unused
    Logical,
}

/// <summary>
/// Saves the name of a <see cref="Parameter"/> and its value.
/// Additionally, information about the bounds of the parameter is saved.
/// </summary>
public class Parameter
{
    #region Fields

    private Number _value;

    private readonly ParameterValidation min;
    private readonly ParameterValidation max;

    #endregion

    #region Constructors

    internal Parameter(Token name, Token value, ParameterValidation minval, ParameterValidation maxval)
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
                Value = Number.FromBooleanLiteral(value);
                break;
            default:
                Debug.Fail("Unreachable: parameter value not a number or boolean!");
                return; // unreachable
        }

        min = minval;
        max = maxval;
        if (min.Contradicts(max) || max.Contradicts(min))
        {
            throw new GenerationException("Contradicting parameter bounds!", value.Line);
        }
        else if (Validate(Value) != 0)
        {
            throw new GenerationException("Default value does not comply with bounds!", value.Line);
        }
    }

    internal Parameter(Parameter old, ParameterType type, Number value)
    {
        Name = old.Name;

        Type = type; // gotta do this before to avoid a bad number for the type
        Value = value;

        min = old.min;
        max = old.max;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Name of this Parameter (/\w+/ with underscores normalized to spaces).
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The type of the parameter.
    /// </summary>
    public ParameterType Type { get; }

    /// <summary>
    /// The current value of this Parameter.
    /// </summary>
    public Number Value
    {
        get => _value;
        set
        {
            switch (Type)
            {
                case ParameterType.Real:
                    _value = value;
                    break;
                case ParameterType.Integer:
                    _value = (int)value;
                    break;
                case ParameterType.Logical:
                    _value = (bool)value;
                    break;
            }
        }
    }

    #endregion

    #region Methods

    /// <summary>
    /// Validates the given value according to the Parameter's indicated bounds.
    /// </summary>
    /// <param name="value">Value to validate</param>
    /// <returns>
    /// equal to 0 -> acceptable value <br/>
    /// less than 0 -> too low value   <br/>
    /// more than 0 -> too high value  <br/>
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
/// Read-Only representation of a <see cref="Parameter"/>.
/// </summary>
/// <param name="Name">The name</param>
/// <param name="Type">The type</param>
/// <param name="Value">The value</param>
public readonly record struct ReadOnlyParameter(string Name, ParameterType Type, Number Value)
{
    internal ReadOnlyParameter(Parameter parameter) : this(parameter.Name, parameter.Type, parameter.Value) {}
}
