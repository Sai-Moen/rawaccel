using System.Collections.Generic;
using System.Diagnostics;

namespace scripting.Lexing;

/// <summary>
/// Enumerates all possible Token types.
/// </summary>
public enum TokenType
{
    Undefined, // Doesn't mean invalid right away, depends on if you expect a certain symbol
    Number, Bool, Constant,
    Input, Output, Identifier, Parameter,
    Immutable, Persistent, Impersistent,
    Const, Let, Var, Fn,
    Return, If, Else, While,
    Terminator, ParenOpen, ParenClose,
    SquareOpen, SquareClose, CurlyOpen, CurlyClose,
    Assignment, Compound, Arithmetic, Comparison,
    Function, FunctionLocal, ArgumentSeparator, MathFunction,
}

/// <summary>
/// Holds the basic requirements for a Token.
/// </summary>
/// <param name="Type">Type of the Token</param>
/// <param name="Symbol">String representation of the Token</param>
public record BaseToken(TokenType Type, string Symbol);

/// <summary>
/// Holds a BaseToken including some extra information.
/// </summary>
/// <param name="Base">The BaseToken</param>
/// <param name="Line">The line in the file where this came from, 0 means unknown</param>
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
    // Zero
    public const string ZERO = "zero";

    // Constants
    public const string CONST_E = "e";
    public const string CONST_PI = "pi";
    public const string CONST_TAU = "tau";
    public const string CONST_CAPACITY = "capacity";

    // Booleans
    public const string FALSE = "false";
    public const string TRUE = "true";

    // Calculation IO
    public const string INPUT = "x";
    public const string OUTPUT = "y";

    // Declarations
    public const string DECL_CONST = "const"; // immutable (so automatically persistent)
    public const string DECL_LET = "let";     // persistent mutable
    public const string DECL_VAR = "var";     // impersistent mutable
    public const string DECL_FN = "fn";       // user-defined function

    // Branches
    public const string RETURN = "ret";
    public const string BRANCH_IF = "if";
    public const string BRANCH_ELSE = "else";
    public const string BRANCH_WHILE = "while";

    // Separators
    // Delimiters
    public const string SPACE = " ";
    public const string UNDER = "_";   // For: spaces in parameter names
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
    public const string C_ADD = "+=";
    public const string C_SUB = "-=";
    public const string C_MUL = "*=";
    public const string C_DIV = "/=";
    public const string C_MOD = "%=";
    public const string C_POW = "^=";

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
    public const string ABS = "abs";            // Absolute Value
    public const string SIGN = "sign";          // Sign
    public const string COPY_SIGN = "copysign"; // Copy Sign

    // Rounding
    public const string ROUND = "round"; // Round to nearest
    public const string TRUNC = "trunc"; // Round to 0
    public const string FLOOR = "floor"; // Round to -infinity
    public const string CEIL = "ceil";   // Round to infinity
    public const string CLAMP = "clamp"; // Clamps second argument between the first and third

    // MinMax
    public const string MIN = "min";            // Minimum of the two arguments
    public const string MAX = "max";            // Maximum of the two arguments
    public const string MIN_MAGNITUDE = "minm"; // Closest to 0 of the two arguments
    public const string MAX_MAGNITUDE = "maxm"; // Furthest from 0 of the two arguments

    // Roots
    public const string SQRT = "sqrt"; // Square Root
    public const string CBRT = "cbrt"; // Cube Root

    // Logarithm
    public const string LOG = "log";      // Natural Logarithm (ln a)
    public const string LOG_2 = "log2";   // Binary Logarithm (log2 a)
    public const string LOG_10 = "log10"; // Decimal Logarithm (log10 a)
    public const string LOG_B = "logb";   // Logarithm with base b (logb a b)
    public const string ILOG_B = "ilogb"; // Binary Logarithm that gets the integer exponent (ilogb a)

    // Sine
    public const string SIN = "sin";     // Normal
    public const string SINH = "sinh";   // Hyperbolic
    public const string ASIN = "asin";   // Inverse
    public const string ASINH = "asinh"; // Inverse Hyperbolic

    // Cosine
    public const string COS = "cos";     // Normal
    public const string COSH = "cosh";   // Hyperbolic
    public const string ACOS = "acos";   // Inverse
    public const string ACOSH = "acosh"; // Inverse Hyperbolic

    // Tangent
    public const string TAN = "tan";     // Normal
    public const string TANH = "tanh";   // Hyperbolic
    public const string ATAN = "atan";   // Inverse
    public const string ATANH = "atanh"; // Inverse Hyperbolic
    public const string ATAN2 = "atan2"; // Angle of which the tangent is y / x

    // Miscellaneous
    public const string FUSED_MULTIPLY_ADD = "fma"; // x * y + z
    public const string SCALE_B = "scaleb";         // Binary Scale (IEEE754 exponent trickery idfk)

    // Premade Tokens
    public static readonly Token DUMMY = new(new(TokenType.Undefined, NONE));

    #endregion

    #region Fields

    private static readonly Dictionary<string, Token> reservedMap;

    private static readonly BaseToken[] ReservedArray =
    [
        // Special untyped 'characters' that show up sometimes
        new(TokenType.Undefined, UNDER),
        new(TokenType.Undefined, SECOND),

        new(TokenType.Input, INPUT),
        new(TokenType.Output, OUTPUT),

        new(TokenType.Constant, ZERO),
        new(TokenType.Constant, CONST_E),
        new(TokenType.Constant, CONST_PI),
        new(TokenType.Constant, CONST_TAU),
        new(TokenType.Constant, CONST_CAPACITY),

        new(TokenType.Bool, FALSE),
        new(TokenType.Bool, TRUE),

        new(TokenType.Const, DECL_CONST),
        new(TokenType.Let, DECL_LET),
        new(TokenType.Var, DECL_VAR),
        new(TokenType.Fn, DECL_FN),

        new(TokenType.Return, RETURN),
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
        new(TokenType.Compound, C_ADD),
        new(TokenType.Compound, C_SUB),
        new(TokenType.Compound, C_MUL),
        new(TokenType.Compound, C_DIV),
        new(TokenType.Compound, C_MOD),
        new(TokenType.Compound, C_POW),

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

        new(TokenType.MathFunction, ABS),
        new(TokenType.MathFunction, SIGN),
        new(TokenType.MathFunction, COPY_SIGN),

        new(TokenType.MathFunction, ROUND),
        new(TokenType.MathFunction, TRUNC),
        new(TokenType.MathFunction, FLOOR),
        new(TokenType.MathFunction, CEIL),
        new(TokenType.MathFunction, CLAMP),

        new(TokenType.MathFunction, MIN),
        new(TokenType.MathFunction, MAX),
        new(TokenType.MathFunction, MIN_MAGNITUDE),
        new(TokenType.MathFunction, MAX_MAGNITUDE),

        new(TokenType.MathFunction, SQRT),
        new(TokenType.MathFunction, CBRT),

        new(TokenType.MathFunction, LOG),
        new(TokenType.MathFunction, LOG_2),
        new(TokenType.MathFunction, LOG_10),
        new(TokenType.MathFunction, LOG_B),
        new(TokenType.MathFunction, ILOG_B),

        new(TokenType.MathFunction, SIN),
        new(TokenType.MathFunction, SINH),
        new(TokenType.MathFunction, ASIN),
        new(TokenType.MathFunction, ASINH),

        new(TokenType.MathFunction, COS),
        new(TokenType.MathFunction, COSH),
        new(TokenType.MathFunction, ACOS),
        new(TokenType.MathFunction, ACOSH),

        new(TokenType.MathFunction, TAN),
        new(TokenType.MathFunction, TANH),
        new(TokenType.MathFunction, ATAN),
        new(TokenType.MathFunction, ATANH),
        new(TokenType.MathFunction, ATAN2),

        new(TokenType.MathFunction, FUSED_MULTIPLY_ADD),
        new(TokenType.MathFunction, SCALE_B),
    ];

    #endregion

    #region Constructors

    static Tokens()
    {
        reservedMap = new(ReservedArray.Length);
        foreach (BaseToken baseToken in ReservedArray)
        {
            reservedMap.Add(baseToken.Symbol, new(baseToken));
        }

        Debug.Assert(reservedMap.Count == ReservedArray.Length);
    }

    #endregion

    #region Static Methods

    public static string Normalize(string s) => s.Replace(UNDER, SPACE);

    public static bool IsReserved(char c) => IsReserved(c.ToString());
    public static bool IsReserved(string symbol) => reservedMap.ContainsKey(symbol);

    public static Token GetReserved(string symbol) => reservedMap[symbol];
    public static Token GetReserved(string symbol, uint line) => reservedMap[symbol] with { Line = line };

    #endregion
}
