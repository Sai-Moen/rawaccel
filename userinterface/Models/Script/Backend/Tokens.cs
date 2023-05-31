global using TokenMap = System.Collections.Generic.IDictionary
    <string, userinterface.Models.Script.Backend.Token>;

using System.Collections.Generic;
using System.Diagnostics;

namespace userinterface.Models.Script.Backend
{
    internal enum TokenType
    {
        Undefined,
        Identifier,
        Parameter,
        Variable,
        Literal,
        Keyword,
        Separator,
        Operator,
        Function,
    }

    internal record Token(TokenType TokenType, string Word);

    internal static class Tokens
    {
        private static readonly (TokenType, string)[] ListReserved = new (TokenType, string)[]
        {
            (Keywords.DefaultType, Keywords.INPUT),
            (Keywords.DefaultType, Keywords.OUTPUT),
            (Keywords.DefaultType, Keywords.CONST_E),
            (Keywords.DefaultType, Keywords.CONST_PI),
            (Keywords.DefaultType, Keywords.CONST_TAU),
            (Keywords.DefaultType, Keywords.BRANCH_IF),
            (Keywords.DefaultType, Keywords.BRANCH_WHILE),

            (Separators.DefaultType, Separators.BLOCK),
            (Separators.DefaultType, Separators.TERMINATOR),
            (Separators.DefaultType, Separators.FPOINT),
            (Separators.DefaultType, Separators.PREC_START),
            (Separators.DefaultType, Separators.PREC_END),
            (Separators.DefaultType, Separators.PARAMS_START),
            (Separators.DefaultType, Separators.PARAMS_END),
            (Separators.DefaultType, Separators.CALC_START),
            (Separators.DefaultType, Separators.CALC_END),

            (Operators.DefaultType, Operators.ASSIGN),
            (Operators.DefaultType, Operators.ADD),
            (Operators.DefaultType, Operators.SUB),
            (Operators.DefaultType, Operators.MUL),
            (Operators.DefaultType, Operators.DIV),
            (Operators.DefaultType, Operators.MOD),
            (Operators.DefaultType, Operators.EXP),
            (Operators.DefaultType, Operators.IADD),
            (Operators.DefaultType, Operators.ISUB),
            (Operators.DefaultType, Operators.IMUL),
            (Operators.DefaultType, Operators.IDIV),
            (Operators.DefaultType, Operators.IMOD),
            (Operators.DefaultType, Operators.CMP_AND),
            (Operators.DefaultType, Operators.CMP_OR),
            (Operators.DefaultType, Operators.CMP_NOT),
            (Operators.DefaultType, Operators.CMP_EQ),
            (Operators.DefaultType, Operators.CMP_LT),
            (Operators.DefaultType, Operators.CMP_GT),
            (Operators.DefaultType, Operators.CMP_LE),
            (Operators.DefaultType, Operators.CMP_GE),

            (Functions.DefaultType, Functions.ABS),
            (Functions.DefaultType, Functions.SQRT),
            (Functions.DefaultType, Functions.CBRT),
            (Functions.DefaultType, Functions.ROUND),
            (Functions.DefaultType, Functions.TRUNC),
            (Functions.DefaultType, Functions.CEIL),
            (Functions.DefaultType, Functions.FLOOR),
            (Functions.DefaultType, Functions.LOG),
            (Functions.DefaultType, Functions.LOG2),
            (Functions.DefaultType, Functions.LOG10),
            (Functions.DefaultType, Functions.SIN),
            (Functions.DefaultType, Functions.SINH),
            (Functions.DefaultType, Functions.ASIN),
            (Functions.DefaultType, Functions.ASINH),
            (Functions.DefaultType, Functions.COS),
            (Functions.DefaultType, Functions.COSH),
            (Functions.DefaultType, Functions.ACOS),
            (Functions.DefaultType, Functions.ACOSH),
            (Functions.DefaultType, Functions.TAN),
            (Functions.DefaultType, Functions.TANH),
            (Functions.DefaultType, Functions.ATAN),
            (Functions.DefaultType, Functions.ATANH),
        };

        static Tokens()
        {
            MapReserved = new Dictionary<string, Token>(ListReserved.Length);

            foreach((TokenType type, string name) in ListReserved)
            {
                MapReserved.Add(name, new Token(type, name));
            }

            Debug.Assert(MapReserved.Count == ListReserved.Length);
        }

        public static readonly TokenMap MapReserved;

        internal static class Keywords
        {
            public const TokenType DefaultType = TokenType.Keyword;

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
            private static readonly string[] SeparatorsList = new string[]
            {
                BLOCK, TERMINATOR, FPOINT,
                PREC_START, PREC_END,
                PARAMS_START, PARAMS_END,
                CALC_START, CALC_END,
            };

            private static readonly string[] CalculationSeparatorsList = new string[]
            {
                BLOCK, TERMINATOR,
                PREC_START, PREC_END,
            };

            static Separators()
            {
                Set = new HashSet<char>();

                foreach(string s in SeparatorsList)
                {
                    Debug.Assert(s.Length == 1);
                }

                foreach(string s in CalculationSeparatorsList)
                {
                    Set.Add(s[0]);
                }
            }

            public static readonly ISet<char> Set;

            public const TokenType DefaultType = TokenType.Separator;

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
            private static readonly string[] OperatorsList = new string[]
            {
                ASSIGN,
                ADD, SUB, MUL, DIV, MOD, EXP,
                IADD, ISUB, IMUL, IDIV, IMOD,
                CMP_AND, CMP_OR, CMP_NOT,
                CMP_EQ, CMP_LT, CMP_GT, CMP_LE, CMP_GE,
            };

            static Operators()
            {
                Set = new HashSet<char>();

                foreach(string s in OperatorsList)
                {
                    // Hack safely,
                    Debug.Assert(s.Length == 1 || (s.Length == 2 && s[1] == SECOND_C));

                    // since we know that the second character is always the same
                    Set.Add(s[0]);
                }
            }

            public static readonly ISet<char> Set;

            public const TokenType DefaultType = TokenType.Operator;

            // Assignment
            public const string ASSIGN  = "=";
            public const char SECOND_C  = '=';

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
            public const TokenType DefaultType = TokenType.Function;

            // General
            public const string ABS     = "abs";    // Absolute Value
            public const string SQRT    = "sqrt";   // Square Root
            public const string CBRT    = "cbrt";   // Cube Root

            // Rounding
            public const string ROUND   = "round";  // Round to nearest
            public const string TRUNC   = "trunc";  // Round to 0
            public const string CEIL    = "ceil";   // Round to infinity
            public const string FLOOR   = "floor";  // Round to -infinity

            // Logarithm
            public const string LOG     = "log";    // Natural Logarithm (loge x)
            public const string LOG2    = "log2";   // Binary Logarithm (log2 x)
            public const string LOG10   = "log10";  // Decimal Logarithm (log10 x)

            // Sine
            public const string SIN     = "sin";    // Normal
            public const string SINH    = "sinh";   // Hyperbolic
            public const string ASIN    = "asin";   // Inverse
            public const string ASINH   = "asinh";  // Inverse Hyperbolic

            // Cosine
            public const string COS     = "cos";    // Normal
            public const string COSH    = "cosh";   // Hyperbolic
            public const string ACOS    = "acos";   // Inverse
            public const string ACOSH   = "acosh";  // Inverse Hyperbolic

            // Tangent
            public const string TAN     = "tan";    // Normal
            public const string TANH    = "tanh";   // Hyperbolic
            public const string ATAN    = "atan";   // Inverse
            public const string ATANH   = "atanh";  // Inverse Hyperbolic
        }
    }
}
