using scripting.Common;
using scripting.Lexical;

namespace scripting.Script;

/// <summary>
/// Represents a number or boolean in the script.
/// </summary>
/// <param name="Value">Value of the Number.</param>
public readonly record struct Number(double Value)
{
    #region Constants

    public const int SIZE = sizeof (double);

    public const double FALSE = 0.0;
    public const double TRUE = 1.0;

    public const double ZERO = 0.0;
    public const double DEFAULT_X = ZERO;
    public const double DEFAULT_Y = 1.0;

    #endregion Constants

    #region Static Methods

    public static Number Parse(string s)
    {
        return Parse(s, new GenerationException("Cannot parse number!"));
    }

    public static Number Parse(string s, uint line)
    {
        return Parse(s, new GenerationException("Cannot parse number!", line));
    }

    private static Number Parse(string s, GenerationException e)
    {
        if (double.TryParse(s, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out double result))
        {
            return result;
        }

        throw e;
    }

    #endregion Static Methods

    #region Operators

    public static implicit operator Number(bool value)
    {
        return Convert.ToDouble(value);
    }

    public static implicit operator Number(double value)
    {
        return new(value);
    }

    public static explicit operator Number(Token token)
    {
        Debug.Assert(token.Base.Type == TokenType.Number);
        return Parse(token.Base.Symbol, token.Line);
    }

    public static implicit operator bool(Number number)
    {
        return number.Value != ZERO;
    }

    public static implicit operator double(Number number)
    {
        return number.Value;
    }

    public static Number operator |(Number left, Number right)
    {
        return left != ZERO | right != ZERO;
    }

    public static Number operator &(Number left, Number right)
    {
        return left != ZERO & right != ZERO;
    }

    public static Number operator !(Number number)
    {
        return number == ZERO;
    }

    #endregion Operators
}
