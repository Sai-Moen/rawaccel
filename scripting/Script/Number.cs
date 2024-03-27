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

    #endregion

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

    #endregion

    #region Operators

    public static implicit operator Number(bool value) => Convert.ToDouble(value);
    public static implicit operator Number(double value) => new(value);

    public static explicit operator Number(Token token)
    {
        Debug.Assert(token.Type == TokenType.Number);
        return Parse(token.Symbol, token.Line);
    }

    public static bool operator false(Number number) => number == ZERO;
    public static bool operator true(Number number) => number != ZERO;

    public static implicit operator bool(Number number) => number != ZERO;
    public static implicit operator double(Number number) => number.Value;

    public static Number operator !(Number number) => number == ZERO;

    public static Number operator |(Number left, Number right) => left != ZERO | right != ZERO;
    public static Number operator &(Number left, Number right) => left != ZERO & right != ZERO;

    #endregion
}
