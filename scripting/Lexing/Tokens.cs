using System.Collections.Generic;
using System.Diagnostics;

namespace scripting.Lexing;

/// <summary>
/// Enumerates all possible Token types.
/// </summary>
public enum TokenType
{
    Undefined, // Doesn't mean invalid right away, depends on if you expect a certain symbol
    Identifier, Number, Parameter, Variable,
    Input, Output, Return,
    Constant, Bool,
    If, Else, While,
    Terminator, ParenOpen, ParenClose,
    SquareOpen, SquareClose, CurlyOpen, CurlyClose,
    Assignment, Arithmetic, Comparison,
    ArgumentSeparator, Function,
}

/// <summary>
/// Holds the basic requirements for a Token.
/// </summary>
/// <param name="Type">Type of the Token.</param>
/// <param name="Symbol">String representation of the Token.</param>
public record BaseToken(TokenType Type, string Symbol);

/// <summary>
/// Holds a BaseToken including some extra information.
/// </summary>
/// <param name="Base">The BaseToken.</param>
/// <param name="Line">The line in the file where this came from. 0 means unknown.</param>
public record Token(BaseToken Base, uint Line = 0)
{
    public TokenType Type => Base.Type;
    public string Symbol => Base.Symbol;
}

/// <summary>
/// Defines all reserved kinds of Tokens.
/// </summary>
public static class Tokens
{
    #region Constants

    // For tokens that are required for context, but are not tokenized otherwise.
    public const string NONE = "";

    // Single line comments
    public const string COMMENT_LINE = "#";

    // Keywords
    // Calculation IO
    public const string INPUT = "x";
    public const string OUTPUT = "y";
    public const string RETURN = "ret";

    // Unary Minus Hack
    public const string ZERO = "zero";

    // Constants
    public const string CONST_E = "e";
    public const string CONST_PI = "pi";
    public const string CONST_TAU = "tau";
    public const string CONST_CAPACITY = "capacity";

    // Booleans
    public const string FALSE = "false";
    public const string TRUE = "true";

    // Branching
    public const string BRANCH_IF = "if";
    public const string BRANCH_ELSE = "else";
    public const string BRANCH_WHILE = "while";
    public const string BRANCH_END = NONE;

    // Separators
    // Delimiters
    public const string SPACE = " ";
    public const string UNDER = "_"; // For: spaces in parameter names
    public const string ARG_SEP = ","; // For: multiple function arguments
    public const string FPOINT = ".";
    public const string TERMINATOR = ";";

    // Precendence
    public const string PAREN_OPEN = "(";
    public const string PAREN_CLOSE = ")";

    // Header (Parameters)
    public const string SQUARE_OPEN = "[";
    public const string SQUARE_CLOSE = "]";

    // Calculation
    public const string CURLY_OPEN = "{";
    public const string CURLY_CLOSE = "}";

    // Operators
    // Assignment
    public const string ASSIGN = ":=";
    public const string SECOND = "="; // if there is a second character, it should be this!

    // Inline Arithmetic
    public const string IADD = "+=";
    public const string ISUB = "-=";
    public const string IMUL = "*=";
    public const string IDIV = "/=";
    public const string IMOD = "%=";
    public const string IPOW = "^=";

    // Normal Arithmetic
    public const string ADD = "+";
    public const string SUB = "-";
    public const string MUL = "*";
    public const string DIV = "/";
    public const string MOD = "%";
    public const string POW = "^";

    // Comparison
    public const string NOT = "!";
    public const string LT = "<";
    public const string GT = ">";
    public const string LE = "<=";
    public const string GE = ">=";
    public const string EQ = "==";
    public const string NE = "!=";
    public const string AND = "&";
    public const string OR = "|";

    // Functions
    // General
    public const string ABS = "abs";      // Absolute Value
    public const string SIGN = "sign";     // Sign
    public const string COPY_SIGN = "copysign"; // Copy Sign

    // Rounding
    public const string ROUND = "round"; // Round to nearest
    public const string TRUNC = "trunc"; // Round to 0
    public const string FLOOR = "floor"; // Round to -infinity
    public const string CEIL = "ceil";  // Round to infinity
    public const string CLAMP = "clamp"; // Clamps second argument between the first and third

    // MinMax
    public const string MIN = "min";  // Minimum of the two arguments
    public const string MAX = "max";  // Maximum of the two arguments
    public const string MIN_MAGNITUDE = "minm"; // Closest to 0 of the two arguments
    public const string MAX_MAGNITUDE = "maxm"; // Furthest from 0 of the two arguments

    // Roots (bloody roots)
    public const string SQRT = "sqrt"; // Square Root
    public const string CBRT = "cbrt"; // Cube Root

    // Logarithm
    public const string LOG = "log";   // Natural Logarithm (loge x)
    public const string LOG_2 = "log2";  // Binary Logarithm (log2 x)
    public const string LOG_10 = "log10"; // Decimal Logarithm (log10 x)
    public const string LOG_B = "logb";  // Logarithm with base b (logb x)

    // Sine
    public const string SIN = "sin";   // Normal
    public const string SINH = "sinh";  // Hyperbolic
    public const string ASIN = "asin";  // Inverse
    public const string ASINH = "asinh"; // Inverse Hyperbolic

    // Cosine
    public const string COS = "cos";   // Normal
    public const string COSH = "cosh";  // Hyperbolic
    public const string ACOS = "acos";  // Inverse
    public const string ACOSH = "acosh"; // Inverse Hyperbolic

    // Tangent
    public const string TAN = "tan";   // Normal
    public const string TANH = "tanh";  // Hyperbolic
    public const string ATAN = "atan";  // Inverse
    public const string ATANH = "atanh"; // Inverse Hyperbolic
    public const string ATAN2 = "atan2"; // Angle of which the tangent is y / x

    // Miscellaneous
    public const string FUSED_MULTIPLY_ADD = "fma";    // x * y + z
    public const string SCALE_B = "scaleb"; // Binary Scale (IEEE754 exponent trickery idfk)

    // Premade Tokens
    public static readonly Token DUMMY = new(new(TokenType.Undefined, NONE));

    #endregion

    #region Fields

    private static readonly BaseToken[] ReservedArray =
    [
        // Special untyped 'characters' that show up sometimes
        new(TokenType.Undefined, UNDER),
        new(TokenType.Undefined, SECOND),

        new(TokenType.Input, INPUT),
        new(TokenType.Output, OUTPUT),
        new(TokenType.Return, RETURN),

        new(TokenType.Constant, ZERO),
        new(TokenType.Constant, CONST_E),
        new(TokenType.Constant, CONST_PI),
        new(TokenType.Constant, CONST_TAU),
        new(TokenType.Constant, CONST_CAPACITY),

        new(TokenType.Bool, FALSE),
        new(TokenType.Bool, TRUE),

        new(TokenType.If, BRANCH_IF),
        new(TokenType.Else, BRANCH_ELSE),
        new(TokenType.While, BRANCH_WHILE),

        new(TokenType.ArgumentSeparator, ARG_SEP),
        new(TokenType.Number, FPOINT),
        new(TokenType.Terminator, TERMINATOR),

        new(TokenType.ParenOpen, PAREN_OPEN),
        new(TokenType.ParenClose, PAREN_CLOSE),

        new(TokenType.SquareOpen, SQUARE_OPEN),
        new(TokenType.SquareClose, SQUARE_CLOSE),

        new(TokenType.CurlyOpen, CURLY_OPEN),
        new(TokenType.CurlyClose, CURLY_CLOSE),

        new(TokenType.Assignment, ASSIGN),
        new(TokenType.Assignment, IADD),
        new(TokenType.Assignment, ISUB),
        new(TokenType.Assignment, IMUL),
        new(TokenType.Assignment, IDIV),
        new(TokenType.Assignment, IMOD),
        new(TokenType.Assignment, IPOW),

        new(TokenType.Arithmetic, ADD),
        new(TokenType.Arithmetic, SUB),
        new(TokenType.Arithmetic, MUL),
        new(TokenType.Arithmetic, DIV),
        new(TokenType.Arithmetic, MOD),
        new(TokenType.Arithmetic, POW),

        new(TokenType.Comparison, NOT),
        new(TokenType.Comparison, LT),
        new(TokenType.Comparison, GT),
        new(TokenType.Comparison, LE),
        new(TokenType.Comparison, GE),
        new(TokenType.Comparison, EQ),
        new(TokenType.Comparison, NE),
        new(TokenType.Comparison, AND),
        new(TokenType.Comparison, OR),

        new(TokenType.Function, ABS),
        new(TokenType.Function, SIGN),
        new(TokenType.Function, COPY_SIGN),

        new(TokenType.Function, ROUND),
        new(TokenType.Function, TRUNC),
        new(TokenType.Function, FLOOR),
        new(TokenType.Function, CEIL),
        new(TokenType.Function, CLAMP),

        new(TokenType.Function, MIN),
        new(TokenType.Function, MAX),
        new(TokenType.Function, MIN_MAGNITUDE),
        new(TokenType.Function, MAX_MAGNITUDE),

        new(TokenType.Function, SQRT),
        new(TokenType.Function, CBRT),

        new(TokenType.Function, LOG),
        new(TokenType.Function, LOG_2),
        new(TokenType.Function, LOG_10),
        new(TokenType.Function, LOG_B),

        new(TokenType.Function, SIN),
        new(TokenType.Function, SINH),
        new(TokenType.Function, ASIN),
        new(TokenType.Function, ASINH),

        new(TokenType.Function, COS),
        new(TokenType.Function, COSH),
        new(TokenType.Function, ACOS),
        new(TokenType.Function, ACOSH),

        new(TokenType.Function, TAN),
        new(TokenType.Function, TANH),
        new(TokenType.Function, ATAN),
        new(TokenType.Function, ATANH),
        new(TokenType.Function, ATAN2),

        new(TokenType.Function, FUSED_MULTIPLY_ADD),
        new(TokenType.Function, SCALE_B),
    ];

    private static Dictionary<string, Token> ReservedMap { get; }

    #endregion

    #region Constructors

    static Tokens()
    {
        ReservedMap = new(ReservedArray.Length);
        foreach (BaseToken baseToken in ReservedArray)
        {
            ReservedMap.Add(baseToken.Symbol, new(baseToken));
        }

        Debug.Assert(ReservedMap.Count == ReservedArray.Length);
    }

    #endregion

    #region Static Methods

    public static string Normalize(string s) => s.Replace(UNDER, SPACE);

    public static bool IsReserved(char c) => IsReserved(c.ToString());
    public static bool IsReserved(string symbol) => ReservedMap.ContainsKey(symbol);

    public static Token GetReserved(string symbol) => ReservedMap[symbol];
    public static Token GetReserved(string symbol, uint line) => ReservedMap[symbol] with { Line = line };

    #endregion

    #region Extension Methods

    // only 'else' is not conditional at the moment
    public static bool IsConditional(this Token token) => token.Type == TokenType.If || token.Type == TokenType.While;

    // only 'while' is a loop at the moment
    public static bool IsLoop(this Token token) => token.Type == TokenType.While;

    #endregion
}
