using System.Collections.Generic;

namespace userspace_backend.ScriptingLanguage.Compiler.Tokenizer;

/// <summary>
/// Enumerates all possible Token types.
/// </summary>
public enum TokenType : byte
{
    None, // Doesn't mean invalid right away, depends on if you expect a certain symbol

    Number, Bool, Constant,
    Input, Output, Identifier, Parameter,
    Immutable, Persistent, Impersistent,
    Const, Let, Var, Fn,
    Return, If, Else, While,
    Terminator, ParenOpen, ParenClose,
    SquareOpen, SquareClose, CurlyOpen, CurlyClose,
    Assignment, Compound, Arithmetic, Comparison,
    Function, FunctionLocal, ArgumentSeparator, MathFunction,

    Count
}

/// <summary>
/// A lexical token.
/// </summary>
/// <param name="Type">Type of token.</param>
/// <param name="Position">
/// Starting position (byte index in script) of the symbol, can be used to determine the line/character in case of an error.
/// </param>
/// <param name="SymbolIndex">Index of the identifier's symbol (or .Invalid if no runtime symbol).</param>
/// <param name="ExtraIndex">
/// For some TokenTypes it's convenient to test if they are broadly a certain type (e.g. MathFunction),
/// but they then need to be subdivided.
/// This property stores any extra information needed to determine the exact subtype that this token represents.
/// </param>
public readonly record struct Token(
    TokenType Type = TokenType.None,
    int Position = -1,
    SymbolIndex SymbolIndex = SymbolIndex.Invalid,
    byte ExtraIndex = 0);

#region ExtraIndices

public enum ExtraIndexSpecial : byte
{
    Underscore, EqualsSign,
}

public enum ExtraIndexConstant : byte
{
    Zero, E, Pi, Tau, Capacity,
}

public enum ExtraIndexCompound : byte
{
    Add, Sub,
    Mul, Div, Mod,
    Pow,
}
// it's possible that non-arithmetic compound operators can be added, keep separate from arithmetic from now

public enum ExtraIndexArithmetic : byte
{
    Add, Sub,
    Mul, Div, Mod,
    Pow,
}

public enum ExtraIndexComparison : byte
{
    Not,
    LessThan, GreaterThan, LessThanOrEqual, GreaterThanOrEqual,
    Equal, NotEqual,
    And, Or,
}

public enum ExtraIndexMathFunction : byte
{
    Abs, Sign, CopySign,
    Round, Trunc, Floor, Ceil, Clamp,
    Min, Max, MinMagnitude, MaxMagnitude,
    Sqrt, Cbrt,
    Log, Log2, Log10, LogB, ILogB,
    Sin, Sinh, Asin, Asinh,
    Cos, Cosh, Acos, Acosh,
    Tan, Tanh, Atan, Atanh, Atan2,
    FusedMultiplyAdd, ScaleB,
}

#endregion

/// <summary>
/// Defines all reserved kinds of Tokens.
/// </summary>
public static class Tokens
{
    #region Constant Strings

    // Single line comments
    public const string COMMENT_LINE = "#";

    // Keywords
    // Zero
    public const string ZERO = "zero";

    // Constants
    public const string CONST_E        = "e";
    public const string CONST_PI       = "pi";
    public const string CONST_TAU      = "tau";
    public const string CONST_CAPACITY = "capacity";

    // Booleans
    public const string FALSE = "false";
    public const string TRUE  = "true";

    // Calculation IO
    public const string INPUT  = "x";
    public const string OUTPUT = "y";

    // Declarations
    public const string DECL_CONST = "const"; // immutable (so automatically persistent)
    public const string DECL_LET   = "let";   // persistent mutable
    public const string DECL_VAR   = "var";   // impersistent mutable
    public const string DECL_FN    = "fn";    // user-defined function

    // Branches
    public const string RETURN       = "return";
    public const string BRANCH_IF    = "if";
    public const string BRANCH_ELSE  = "else";
    public const string BRANCH_WHILE = "while";

    // Separators
    // Delimiters
    public const string SPACE      = " ";
    public const string UNDERSCORE = "_"; // For: spaces in parameter names
    public const string ARG_SEP    = ","; // For: multiple function arguments
    public const string FPOINT     = ".";
    public const string TERMINATOR = ";";

    // Precendence
    public const string PAREN_OPEN  = "(";
    public const string PAREN_CLOSE = ")";

    // Header (Parameters)
    public const string SQUARE_OPEN  = "[";
    public const string SQUARE_CLOSE = "]";

    // Calculation
    public const string CURLY_OPEN  = "{";
    public const string CURLY_CLOSE = "}";

    // Operators
    // Assignment
    public const string ASSIGN      = ":=";
    public const string EQUALS_SIGN = "="; // if there is a second character in an operator, it should be this!

    // Compound Arithmetic
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
    public const string AND = "&";
    public const string OR  = "|";
    public const string LT  = "<";
    public const string GT  = ">";
    public const string LE  = "<=";
    public const string GE  = ">=";
    public const string EQ  = "==";
    public const string NE  = "!=";

    // Functions
    // General
    public const string ABS       = "abs";      // Absolute Value
    public const string SIGN      = "sign";     // Sign
    public const string COPY_SIGN = "copysign"; // Copy Sign

    // Rounding
    public const string ROUND = "round"; // Round to nearest
    public const string TRUNC = "trunc"; // Round to 0
    public const string FLOOR = "floor"; // Round to -infinity
    public const string CEIL  = "ceil";  // Round to infinity
    public const string CLAMP = "clamp"; // Clamps second argument between the first and third

    // MinMax
    public const string MIN           = "min";  // Minimum of the two arguments
    public const string MAX           = "max";  // Maximum of the two arguments
    public const string MIN_MAGNITUDE = "minm"; // Closest to 0 of the two arguments
    public const string MAX_MAGNITUDE = "maxm"; // Furthest from 0 of the two arguments

    // Roots
    public const string SQRT = "sqrt"; // Square Root
    public const string CBRT = "cbrt"; // Cube Root

    // Logarithm
    public const string LOG    = "log";   // Natural Logarithm (ln a)
    public const string LOG_2  = "log2";  // Binary Logarithm (log2 a)
    public const string LOG_10 = "log10"; // Decimal Logarithm (log10 a)
    public const string LOG_B  = "logb";  // Logarithm with base b (logb a b)
    public const string ILOG_B = "ilogb"; // Binary Logarithm that gets the integer exponent (ilogb a)

    // Sine
    public const string SIN   = "sin";   // Normal
    public const string SINH  = "sinh";  // Hyperbolic
    public const string ASIN  = "asin";  // Inverse
    public const string ASINH = "asinh"; // Inverse Hyperbolic

    // Cosine
    public const string COS   = "cos";   // Normal
    public const string COSH  = "cosh";  // Hyperbolic
    public const string ACOS  = "acos";  // Inverse
    public const string ACOSH = "acosh"; // Inverse Hyperbolic

    // Tangent
    public const string TAN   = "tan";   // Normal
    public const string TANH  = "tanh";  // Hyperbolic
    public const string ATAN  = "atan";  // Inverse
    public const string ATANH = "atanh"; // Inverse Hyperbolic
    public const string ATAN2 = "atan2"; // Angle of which the tangent is y / x

    // Miscellaneous
    public const string FUSED_MULTIPLY_ADD = "fma";    // x * y + z
    public const string SCALE_B            = "scaleb"; // Binary Scale (IEEE754 exponent trickery idfk)

    #endregion

    public static readonly Token DUMMY = default;

    private static readonly Dictionary<string, Token> reservedMap = new()
    {
        [UNDERSCORE]  = new(TokenType.None, ExtraIndex: (byte)ExtraIndexSpecial.Underscore),
        [EQUALS_SIGN] = new(TokenType.None, ExtraIndex: (byte)ExtraIndexSpecial.EqualsSign),

        [INPUT]  = new(TokenType.Input),
        [OUTPUT] = new(TokenType.Output),

        [ZERO]           = new(TokenType.Constant, ExtraIndex: (byte)ExtraIndexConstant.Zero),
        [CONST_E]        = new(TokenType.Constant, ExtraIndex: (byte)ExtraIndexConstant.E),
        [CONST_PI]       = new(TokenType.Constant, ExtraIndex: (byte)ExtraIndexConstant.Pi),
        [CONST_TAU]      = new(TokenType.Constant, ExtraIndex: (byte)ExtraIndexConstant.Tau),
        [CONST_CAPACITY] = new(TokenType.Constant, ExtraIndex: (byte)ExtraIndexConstant.Capacity),

        [FALSE] = new(TokenType.Bool, ExtraIndex: 0),
        [TRUE]  = new(TokenType.Bool, ExtraIndex: 1),

        [DECL_CONST]   = new(TokenType.Const),
        [DECL_LET]     = new(TokenType.Let),
        [DECL_VAR]     = new(TokenType.Var),
        [DECL_FN]      = new(TokenType.Fn),
        [RETURN]       = new(TokenType.Return),
        [BRANCH_IF]    = new(TokenType.If),
        [BRANCH_ELSE]  = new(TokenType.Else),
        [BRANCH_WHILE] = new(TokenType.While),
        [ARG_SEP]      = new(TokenType.ArgumentSeparator),
        [FPOINT]       = new(TokenType.Number),
        [TERMINATOR]   = new(TokenType.Terminator),
        [PAREN_OPEN]   = new(TokenType.ParenOpen),
        [PAREN_CLOSE]  = new(TokenType.ParenClose),
        [SQUARE_OPEN]  = new(TokenType.SquareOpen),
        [SQUARE_CLOSE] = new(TokenType.SquareClose),
        [CURLY_OPEN]   = new(TokenType.CurlyOpen),
        [CURLY_CLOSE]  = new(TokenType.CurlyClose),
        [ASSIGN]       = new(TokenType.Assignment),

        [C_ADD] = new(TokenType.Compound, ExtraIndex: (byte)ExtraIndexCompound.Add),
        [C_SUB] = new(TokenType.Compound, ExtraIndex: (byte)ExtraIndexCompound.Sub),
        [C_MUL] = new(TokenType.Compound, ExtraIndex: (byte)ExtraIndexCompound.Mul),
        [C_DIV] = new(TokenType.Compound, ExtraIndex: (byte)ExtraIndexCompound.Div),
        [C_MOD] = new(TokenType.Compound, ExtraIndex: (byte)ExtraIndexCompound.Mod),
        [C_POW] = new(TokenType.Compound, ExtraIndex: (byte)ExtraIndexCompound.Pow),

        [ADD] = new(TokenType.Arithmetic, ExtraIndex: (byte)ExtraIndexArithmetic.Add),
        [SUB] = new(TokenType.Arithmetic, ExtraIndex: (byte)ExtraIndexArithmetic.Sub),
        [MUL] = new(TokenType.Arithmetic, ExtraIndex: (byte)ExtraIndexArithmetic.Mul),
        [DIV] = new(TokenType.Arithmetic, ExtraIndex: (byte)ExtraIndexArithmetic.Div),
        [MOD] = new(TokenType.Arithmetic, ExtraIndex: (byte)ExtraIndexArithmetic.Mod),
        [POW] = new(TokenType.Arithmetic, ExtraIndex: (byte)ExtraIndexArithmetic.Pow),

        [NOT] = new(TokenType.Comparison, ExtraIndex: (byte)ExtraIndexComparison.Not),
        [AND] = new(TokenType.Comparison, ExtraIndex: (byte)ExtraIndexComparison.And),
        [OR]  = new(TokenType.Comparison, ExtraIndex: (byte)ExtraIndexComparison.Or),
        [LT]  = new(TokenType.Comparison, ExtraIndex: (byte)ExtraIndexComparison.LessThan),
        [GT]  = new(TokenType.Comparison, ExtraIndex: (byte)ExtraIndexComparison.GreaterThan),
        [LE]  = new(TokenType.Comparison, ExtraIndex: (byte)ExtraIndexComparison.LessThanOrEqual),
        [GE]  = new(TokenType.Comparison, ExtraIndex: (byte)ExtraIndexComparison.GreaterThanOrEqual),
        [EQ]  = new(TokenType.Comparison, ExtraIndex: (byte)ExtraIndexComparison.Equal),
        [NE]  = new(TokenType.Comparison, ExtraIndex: (byte)ExtraIndexComparison.NotEqual),

        [ABS]                = new(TokenType.MathFunction, ExtraIndex: (byte)ExtraIndexMathFunction.Abs),
        [SIGN]               = new(TokenType.MathFunction, ExtraIndex: (byte)ExtraIndexMathFunction.Sign),
        [COPY_SIGN]          = new(TokenType.MathFunction, ExtraIndex: (byte)ExtraIndexMathFunction.CopySign),
        [ROUND]              = new(TokenType.MathFunction, ExtraIndex: (byte)ExtraIndexMathFunction.Round),
        [TRUNC]              = new(TokenType.MathFunction, ExtraIndex: (byte)ExtraIndexMathFunction.Trunc),
        [FLOOR]              = new(TokenType.MathFunction, ExtraIndex: (byte)ExtraIndexMathFunction.Floor),
        [CEIL]               = new(TokenType.MathFunction, ExtraIndex: (byte)ExtraIndexMathFunction.Ceil),
        [CLAMP]              = new(TokenType.MathFunction, ExtraIndex: (byte)ExtraIndexMathFunction.Clamp),
        [MIN]                = new(TokenType.MathFunction, ExtraIndex: (byte)ExtraIndexMathFunction.Min),
        [MAX]                = new(TokenType.MathFunction, ExtraIndex: (byte)ExtraIndexMathFunction.Max),
        [MIN_MAGNITUDE]      = new(TokenType.MathFunction, ExtraIndex: (byte)ExtraIndexMathFunction.MinMagnitude),
        [MAX_MAGNITUDE]      = new(TokenType.MathFunction, ExtraIndex: (byte)ExtraIndexMathFunction.MaxMagnitude),
        [SQRT]               = new(TokenType.MathFunction, ExtraIndex: (byte)ExtraIndexMathFunction.Sqrt),
        [CBRT]               = new(TokenType.MathFunction, ExtraIndex: (byte)ExtraIndexMathFunction.Cbrt),
        [LOG]                = new(TokenType.MathFunction, ExtraIndex: (byte)ExtraIndexMathFunction.Log),
        [LOG_2]              = new(TokenType.MathFunction, ExtraIndex: (byte)ExtraIndexMathFunction.Log2),
        [LOG_10]             = new(TokenType.MathFunction, ExtraIndex: (byte)ExtraIndexMathFunction.Log10),
        [LOG_B]              = new(TokenType.MathFunction, ExtraIndex: (byte)ExtraIndexMathFunction.LogB),
        [ILOG_B]             = new(TokenType.MathFunction, ExtraIndex: (byte)ExtraIndexMathFunction.ILogB),
        [SIN]                = new(TokenType.MathFunction, ExtraIndex: (byte)ExtraIndexMathFunction.Sin),
        [SINH]               = new(TokenType.MathFunction, ExtraIndex: (byte)ExtraIndexMathFunction.Sinh),
        [ASIN]               = new(TokenType.MathFunction, ExtraIndex: (byte)ExtraIndexMathFunction.Asin),
        [ASINH]              = new(TokenType.MathFunction, ExtraIndex: (byte)ExtraIndexMathFunction.Asinh),
        [COS]                = new(TokenType.MathFunction, ExtraIndex: (byte)ExtraIndexMathFunction.Cos),
        [COSH]               = new(TokenType.MathFunction, ExtraIndex: (byte)ExtraIndexMathFunction.Cosh),
        [ACOS]               = new(TokenType.MathFunction, ExtraIndex: (byte)ExtraIndexMathFunction.Acos),
        [ACOSH]              = new(TokenType.MathFunction, ExtraIndex: (byte)ExtraIndexMathFunction.Acosh),
        [TAN]                = new(TokenType.MathFunction, ExtraIndex: (byte)ExtraIndexMathFunction.Tan),
        [TANH]               = new(TokenType.MathFunction, ExtraIndex: (byte)ExtraIndexMathFunction.Tanh),
        [ATAN]               = new(TokenType.MathFunction, ExtraIndex: (byte)ExtraIndexMathFunction.Atan),
        [ATANH]              = new(TokenType.MathFunction, ExtraIndex: (byte)ExtraIndexMathFunction.Atanh),
        [ATAN2]              = new(TokenType.MathFunction, ExtraIndex: (byte)ExtraIndexMathFunction.Atan2),
        [FUSED_MULTIPLY_ADD] = new(TokenType.MathFunction, ExtraIndex: (byte)ExtraIndexMathFunction.FusedMultiplyAdd),
        [SCALE_B]            = new(TokenType.MathFunction, ExtraIndex: (byte)ExtraIndexMathFunction.ScaleB),
    };

    public static string Normalize(string s) => s.Replace(UNDERSCORE, SPACE);

    public static bool IsReserved(char c) => IsReserved(c.ToString());
    public static bool IsReserved(string symbol) => reservedMap.ContainsKey(symbol);

    public static Token GetReserved(string symbol) => reservedMap[symbol];
    public static Token GetReserved(string symbol, int position) => GetReserved(symbol) with { Position = position };
}
