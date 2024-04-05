using scripting.Common;
using scripting.Lexical;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace scripting.Script;

enum Bound
{
    None,
    LowerExcl,
    LowerIncl,
    UpperExcl,
    UpperIncl,
}

record ParameterValidation(Bound Type = Bound.None, Number Value = default)
{
    internal int Validate(Number number) => IsValid(number) ? 1 : 0;

    internal bool Contradicts(ParameterValidation other) => other.Type != Bound.None && !IsValid(other.Value);

    internal bool IsValid(Number number) => Type switch
    {
        Bound.None => true,
        Bound.LowerExcl => Value  <  number,
        Bound.LowerIncl => Value  <= number,
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
                Value = value.FromBoolean();
                break;
            default:
                Debug.Fail("Unreachable: parameter value not a number or boolean!");
                return; // unreachable
        }

        min = minval;
        max = maxval;
        if (Validate(Value) != 0)
        {
            throw new GenerationException("Default value does not comply with guards!", value.Line);
        }
        else if (min.Contradicts(max) || max.Contradicts(min))
        {
            throw new GenerationException("Contradicting parameter bounds!", name.Line);
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

    /// <summary>
    /// The type of the parameter.
    /// </summary>
    public ParameterType Type { get; }

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