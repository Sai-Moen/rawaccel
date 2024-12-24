using System;
using System.Diagnostics;
using System.Globalization;
using userspace_backend.ScriptingLanguage.Compiler.Tokenizer;

namespace userspace_backend.ScriptingLanguage.Script;

/// <summary>
/// Represents a number or boolean in the script.
/// </summary>
/// <param name="Value">Value of the Number.</param>
public readonly record struct Number(double Value)
{
    public const int SIZE = sizeof(double);

    public const double FALSE = 0.0;
    public const double TRUE = 1.0;

    public const double ZERO = 0.0;
    public const double DEFAULT_X = ZERO;
    public const double DEFAULT_Y = 1.0;

    public static Number Parse(string s)
    {
        return Parse(s, new CompilationException("Cannot parse number!"));
    }

    public static Number Parse(string s, Token suspect)
    {
        return Parse(s, new CompilationException("Cannot parse number!", suspect));
    }

    private static Number Parse(string s, CompilationException e)
    {
        if (double.TryParse(s, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out double result))
        {
            return result;
        }

        throw e;
    }

    public static Number FromBooleanLiteral(Token token)
    {
        Debug.Assert(token.Type == TokenType.Bool);
        return token.ExtraIndex switch
        {
            0 => FALSE,
            1 => TRUE,

            _ => throw new CompilationException($"Unknown Bool ExtraIndex value: {token.ExtraIndex}", token)
        };
    }

    public static implicit operator Number(bool value) => Convert.ToDouble(value);
    public static implicit operator Number(double value) => new(value);

    public static implicit operator bool(Number number) => number != ZERO;
    public static implicit operator double(Number number) => number.Value;

    public static bool operator false(Number number) => number == ZERO;
    public static bool operator true(Number number) => number != ZERO;

    public static Number operator !(Number number) => number == ZERO;

    public static Number operator |(Number left, Number right) => left != ZERO | right != ZERO;
    public static Number operator &(Number left, Number right) => left != ZERO & right != ZERO;
}
