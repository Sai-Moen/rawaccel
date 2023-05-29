namespace userinterface.Models.Script.Backend
{
    internal enum TokenType
    {
        Undefined,
        Parameter,
        Variable,
        Keyword,
        Separator,
        Operator,
        Function,
        Literal,
    }

    internal record Token(string Word, TokenType TokenType);

    internal static class Tokens
    {
        public static readonly string[] AllTokens = new string[]
        {
            Keywords.INPUT,
            Keywords.OUTPUT,
            Keywords.CONST_E,
            Keywords.CONST_PI,
            Keywords.CONST_TAU,
            Keywords.BRANCH_IF,
            Keywords.BRANCH_WHILE,

            Separators.BLOCK,
            Separators.TERMINATOR,
            Separators.FPOINT,
            Separators.PREC_START,
            Separators.PREC_END,
            Separators.PARAMS_START,
            Separators.PARAMS_END,
            Separators.CALC_START,
            Separators.CALC_END,

            Operators.ASSIGNMENT,
            Operators.ADD,
            Operators.SUB,
            Operators.MUL,
            Operators.DIV,
            Operators.MOD,
            Operators.EXP,
            Operators.IADD,
            Operators.ISUB,
            Operators.IMUL,
            Operators.IDIV,
            Operators.IMOD,
            Operators.CMP_AND,
            Operators.CMP_OR,
            Operators.CMP_NOT,
            Operators.CMP_EQ,
            Operators.CMP_LT,
            Operators.CMP_GT,
            Operators.CMP_LE,
            Operators.CMP_GE,

            Functions.ABS,
            Functions.CEIL,
            Functions.FLOOR,
            Functions.SQRT,
            Functions.CBRT,
            Functions.LOG,
            Functions.LOG2,
            Functions.LOG10,
            Functions.SIN,
            Functions.SINH,
            Functions.ASIN,
            Functions.ASINH,
            Functions.COS,
            Functions.COSH,
            Functions.ACOS,
            Functions.ACOSH,
            Functions.TAN,
            Functions.TANH,
            Functions.ATAN,
            Functions.ATANH,
            Functions.ATAN2,
        };

        internal static class Keywords
        {
            public const TokenType DefaultTokenType = TokenType.Keyword;

            // Calculation IO
            public const string INPUT           = "x";
            public const string OUTPUT          = "y";

            // Constants
            public const string CONST_E         = "e";
            public const string CONST_PI        = "pi";
            public const string CONST_TAU       = "tau";

            // Branching
            public const string BRANCH_IF       = "if";
            public const string BRANCH_WHILE    = "while";
        }

        internal static class Separators
        {
            public const TokenType DefaultTokenType = TokenType.Separator;

            // Delimiters
            public const string BLOCK           = ":";
            public const string TERMINATOR      = ";";
            public const string FPOINT          = ".";

            // Precendence
            public const string PREC_START      = "(";
            public const string PREC_END        = ")";

            // Header (Parameters)
            public const string PARAMS_START    = "[";
            public const string PARAMS_END      = "]";

            // Calculation
            public const string CALC_START      = "{";
            public const string CALC_END        = "}";
        }

        internal static class Operators
        {
            public const TokenType DefaultTokenType = TokenType.Operator;

            // Assignment
            public const string ASSIGNMENT  = "=";

            // Normal Arithmetic
            public const string ADD     = "+";
            public const string SUB     = "-";
            public const string MUL     = "*";
            public const string DIV     = "/";
            public const string MOD     = "%";
            public const string EXP     = "^";

            // Inline Arithmetic
            public const string IADD    = "+=";
            public const string ISUB    = "-=";
            public const string IMUL    = "*=";
            public const string IDIV    = "/=";
            public const string IMOD    = "%=";

            // Logical
            public const string CMP_AND = "&";
            public const string CMP_OR  = "|";
            public const string CMP_NOT = "!";

            // Comparison
            public const string CMP_EQ  = "==";
            public const string CMP_LT  = "<";
            public const string CMP_GT  = ">";
            public const string CMP_LE  = "<=";
            public const string CMP_GE  = ">=";
        }

        internal static class Functions
        {
            public const TokenType DefaultTokenType = TokenType.Function;

            // General
            public const string ABS     = "abs";
            public const string CEIL    = "ceil";
            public const string FLOOR   = "floor";

            // Roots
            public const string SQRT    = "sqrt";
            public const string CBRT    = "cbrt";

            // Logarithm
            public const string LOG     = "log";
            public const string LOG2    = "log2";
            public const string LOG10   = "log10";

            // Sine
            public const string SIN     = "sin";
            public const string SINH    = "sinh";
            public const string ASIN    = "asin";
            public const string ASINH   = "asinh";

            // Cosine
            public const string COS     = "cos";
            public const string COSH    = "cosh";
            public const string ACOS    = "acos";
            public const string ACOSH   = "acosh";

            // Tangent
            public const string TAN     = "tan";
            public const string TANH    = "tanh";
            public const string ATAN    = "atan";
            public const string ATANH   = "atanh";
            public const string ATAN2   = "atan2";
        }
    }
}
