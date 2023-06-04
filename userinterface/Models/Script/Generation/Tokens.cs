using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace userinterface.Models.Script.Generation
{
    [Flags]
    public enum TokenType : byte
    {
        Undefined   = 0,
        Identifier  = 1 << 0,
        Parameter   = 1 << 1,
        Variable    = 1 << 2,
        Number      = 1 << 3,
        Keyword     = 1 << 4,
        Separator   = 1 << 5,
        Operator    = 1 << 6,
        Function    = 1 << 7,
    }

    public record Token(TokenType Type, uint Line, string Symbol);

    public static class Tokens
    {
        private static readonly (TokenType, string)[] ReservedArray =
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
            (Operators.DefaultType, Operators.IEXP),
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
            ReservedMap = new TokenMap(ReservedArray.Length);

            foreach((TokenType type, string name) in ReservedArray)
            {
                ReservedMap.Add(name, new Token(type, 0, name));
            }

            Debug.Assert(ReservedMap.Count == ReservedArray.Length);
        }

        public static readonly TokenMap ReservedMap;

        public static class Keywords
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

            private static readonly string[] BuiltinArray =
            {
                INPUT, OUTPUT,
                CONST_E, CONST_PI, CONST_TAU,
            };

            private static readonly string[] IOArray =
            {
                INPUT, OUTPUT,
            };

            private static readonly string[] BranchArray =
            {
                BRANCH_IF, BRANCH_WHILE,
            };

            public static readonly TokenSet BuiltinSet = new(BuiltinArray);

            public static readonly TokenSet InOutSet = new(IOArray);

            public static readonly TokenSet BranchSet = new(BranchArray);
        }

        public static class Separators
        {
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

            private static readonly string[] SeparatorsArray =
            {
                BLOCK, TERMINATOR, FPOINT,
                PREC_START, PREC_END,
                PARAMS_START, PARAMS_END,
                CALC_START, CALC_END,
            };

            private static readonly string[] CalcSeparatorsArray =
            {
                BLOCK, TERMINATOR,
                PREC_START, PREC_END,
            };

            static Separators()
            {
                CalcSet = new HashSet<char>();

                foreach(string s in SeparatorsArray)
                {
                    Debug.Assert(s.Length == 1);
                }

                foreach(string s in CalcSeparatorsArray)
                {
                    CalcSet.Add(s[0]);
                }
            }

            public static readonly ISet<char> CalcSet;
        }

        public static class Operators
        {
            public const TokenType DefaultType = TokenType.Operator;

            public const uint MaxPrecedence = 2;

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
            public const string IEXP    = "^=";

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

            private static readonly string[] OperatorsArray =
            {
                ASSIGN,
                ADD, SUB, MUL, DIV, MOD, EXP,
                IADD, ISUB, IMUL, IDIV, IMOD, IEXP,
                CMP_AND, CMP_OR, CMP_NOT,
                CMP_EQ, CMP_LT, CMP_GT, CMP_LE, CMP_GE,
            };

            private static readonly string[][] ArithmeticArray =
            {
                new string[2] { ADD, SUB, },
                new string[3] { MUL, DIV, MOD, },
                new string[1] { EXP, }
            };

            private static readonly string[] AssignmentArray =
            {
                ASSIGN,
                IADD, ISUB, IMUL, IDIV, IMOD, IEXP,
            };

            private static readonly string[] ComparisonArray =
            {
                CMP_AND, CMP_OR, CMP_NOT,
                CMP_EQ, CMP_LT, CMP_GT, CMP_LE, CMP_GE,
            };

            static Operators()
            {
                FullSet = new HashSet<char>();

                foreach(string s in OperatorsArray)
                {
                    // Hack safely,
                    Debug.Assert(s.Length == 1 || (s.Length == 2 && s[1] == SECOND_C));

                    // since we know that the second character is always the same
                    FullSet.Add(s[0]);
                }
            }

            public static readonly ISet<char> FullSet;

            public static readonly TokenSet[] ArithmeticSetArray =
            {
                new(ArithmeticArray[0]),
                new(ArithmeticArray[1]),
                new(ArithmeticArray[2]),
            };

            public static readonly TokenSet AssignmentSet = new(AssignmentArray);

            public static readonly TokenSet ComparisonSet = new(ComparisonArray);
        }

        public static class Functions
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

            private static readonly string[] FunctionsArray =
            {
                ABS, SQRT, CBRT,
                ROUND, TRUNC, CEIL, FLOOR,
                LOG, LOG2, LOG10,
                SIN, SINH, ASIN, ASINH,
                COS, COSH, ACOS, ACOSH,
                TAN, TANH, ATAN, ATANH,
            };

            public static readonly TokenSet FunctionsSet = new(FunctionsArray);
        }
    }

    public class TokenMap : Dictionary<string, Token>, IDictionary<string, Token>
    {
        public TokenMap() : base() {}

        public TokenMap(int capacity) : base(capacity) {}
    }

    public class TokenList : List<Token>, IList<Token>
    {
        public TokenList() : base() {}
    }

    public class TokenSet : HashSet<string>, ISet<string>
    {
        public TokenSet(IEnumerable<string> collection) : base(collection) {}
    }
}
