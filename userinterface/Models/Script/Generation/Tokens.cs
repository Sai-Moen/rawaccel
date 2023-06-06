using System.Collections.Generic;
using System.Diagnostics;

namespace userinterface.Models.Script.Generation
{
    public enum TokenType
    {
        Undefined,

        Identifier,
        Number,
        Parameter,
        Variable,

        Input,
        Output,
        Constant,
        Branch,

        Terminator,
        Block,
        Open,
        Close,

        ParameterStart,
        ParameterEnd,
        CalculationStart,
        CalculationEnd,

        Assignment,
        Arithmetic,
        Comparison,
        Logical,

        Function,
    }

    public record BaseToken(TokenType Type, string Symbol);

    public record Token(BaseToken Base, uint Line = 0);

    public enum Section
    {
        Comments,
        Parameters,
        Variables,
        Calculation,
    }

    public static class Tokens
    {
        #region Constants

        // Keywords
        public const int MAX_PARAMETERS = 8;
        // Calculation IO
        public const string INPUT           = "x";
        public const string OUTPUT          = "y";

        // Unary Minus Hack
        public const string ZERO =          "zero";

        // Constants
        public const string CONST_E         = "e";
        public const string CONST_PI        = "pi";
        public const string CONST_TAU       = "tau";

        // Branching
        public const string BRANCH_IF       = "if";
        public const string BRANCH_WHILE    = "while";

        // Separators
        // Delimiters
        public const string FPOINT          = ".";
        public const string TERMINATOR      = ";";
        public const string BLOCK           = ":";

        // Precendence
        public const string OPEN            = "(";
        public const string CLOSE           = ")";

        // Header (Parameters)
        public const string PARAMS_START    = "[";
        public const string PARAMS_END      = "]";

        // Calculation
        public const string CALC_START      = "{";
        public const string CALC_END        = "}";

        // Operators
        public const uint MAX_PRECEDENCE = 2;
        // Assignment
        public const string ASSIGN  = "=";
        public const char SECOND = '='; // assume the second character of an operator is 2 char
        // Inline Arithmetic
        public const string IADD    = "+=";
        public const string ISUB    = "-=";
        public const string IMUL    = "*=";
        public const string IDIV    = "/=";
        public const string IMOD    = "%=";
        public const string IEXP    = "^=";

        // Normal Arithmetic
        public const string ADD     = "+";
        public const string SUB     = "-";
        public const string MUL     = "*";
        public const string DIV     = "/";
        public const string MOD     = "%";
        public const string EXP     = "^";

        // Comparison
        public const string CMP_EQ  = "==";
        public const string CMP_LT  = "<";
        public const string CMP_GT  = ">";
        public const string CMP_LE  = "<=";
        public const string CMP_GE  = ">=";

        // Logical
        public const string CMP_AND = "&";
        public const string CMP_OR  = "|";
        public const string CMP_NOT = "!";

        // Functions
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

        #endregion Constants

        #region Fields

        private static readonly BaseToken[] ReservedArray =
        {
            new BaseToken(TokenType.Input, INPUT),
            new BaseToken(TokenType.Output, OUTPUT),

            new BaseToken(TokenType.Constant, ZERO),
            new BaseToken(TokenType.Constant, CONST_E),
            new BaseToken(TokenType.Constant, CONST_PI),
            new BaseToken(TokenType.Constant, CONST_TAU),

            new BaseToken(TokenType.Branch, BRANCH_IF),
            new BaseToken(TokenType.Branch, BRANCH_WHILE),

            new BaseToken(TokenType.Number, FPOINT),
            new BaseToken(TokenType.Terminator, TERMINATOR),
            new BaseToken(TokenType.Block, BLOCK),

            new BaseToken(TokenType.Open, OPEN),
            new BaseToken(TokenType.Close, CLOSE),

            new BaseToken(TokenType.ParameterStart, PARAMS_START),
            new BaseToken(TokenType.ParameterEnd, PARAMS_END),

            new BaseToken(TokenType.CalculationStart, CALC_START),
            new BaseToken(TokenType.CalculationEnd, CALC_END),

            new BaseToken(TokenType.Assignment, ASSIGN),
            new BaseToken(TokenType.Assignment, IADD),
            new BaseToken(TokenType.Assignment, ISUB),
            new BaseToken(TokenType.Assignment, IMUL),
            new BaseToken(TokenType.Assignment, IDIV),
            new BaseToken(TokenType.Assignment, IMOD),
            new BaseToken(TokenType.Assignment, IEXP),

            new BaseToken(TokenType.Arithmetic, ADD),
            new BaseToken(TokenType.Arithmetic, SUB),
            new BaseToken(TokenType.Arithmetic, MUL),
            new BaseToken(TokenType.Arithmetic, DIV),
            new BaseToken(TokenType.Arithmetic, MOD),
            new BaseToken(TokenType.Arithmetic, EXP),

            new BaseToken(TokenType.Comparison, CMP_EQ),
            new BaseToken(TokenType.Comparison, CMP_LT),
            new BaseToken(TokenType.Comparison, CMP_GT),
            new BaseToken(TokenType.Comparison, CMP_LE),
            new BaseToken(TokenType.Comparison, CMP_GE),

            new BaseToken(TokenType.Logical, CMP_AND),
            new BaseToken(TokenType.Logical, CMP_OR),
            new BaseToken(TokenType.Logical, CMP_NOT),

            new BaseToken(TokenType.Function, ABS),
            new BaseToken(TokenType.Function, SQRT),
            new BaseToken(TokenType.Function, CBRT),

            new BaseToken(TokenType.Function, ROUND),
            new BaseToken(TokenType.Function, TRUNC),
            new BaseToken(TokenType.Function, CEIL),
            new BaseToken(TokenType.Function, FLOOR),

            new BaseToken(TokenType.Function, LOG),
            new BaseToken(TokenType.Function, LOG2),
            new BaseToken(TokenType.Function, LOG10),

            new BaseToken(TokenType.Function, SIN),
            new BaseToken(TokenType.Function, SINH),
            new BaseToken(TokenType.Function, ASIN),
            new BaseToken(TokenType.Function, ASINH),

            new BaseToken(TokenType.Function, COS),
            new BaseToken(TokenType.Function, COSH),
            new BaseToken(TokenType.Function, ACOS),
            new BaseToken(TokenType.Function, ACOSH),

            new BaseToken(TokenType.Function, TAN),
            new BaseToken(TokenType.Function, TANH),
            new BaseToken(TokenType.Function, ATAN),
            new BaseToken(TokenType.Function, ATANH),
        };

        #endregion Fields

        #region Constructors

        static Tokens()
        {
            // Reserve tokens
            ReservedMap = new TokenMap(ReservedArray.Length);

            foreach(BaseToken baseToken in ReservedArray)
            {
                ReservedMap.Add(baseToken.Symbol, new Token(baseToken));
            }

            // Static asserts
            Debug.Assert(ReservedMap.Count == ReservedArray.Length);
        }

        #endregion Constructors

        #region Properties

        public static readonly TokenMap ReservedMap;

        #endregion Properties

        #region Methods

        public static int Precedence(string s) =>
            s switch
            {
                ADD => 0,
                SUB => 0,
                MUL => 1,
                DIV => 1,
                MOD => 1,
                EXP => 2,

                _ => -1,
        };

        public static bool LeftAssociative(string s) =>
            s switch
            {
                EXP => false,

                _ => true,
        };

        #endregion Methods
    }

    public class TokenMap : Dictionary<string, Token>, IDictionary<string, Token>
    {
        public TokenMap() : base() {}

        public TokenMap(int capacity) : base(capacity) {}
    }

    public class TokenList : List<Token>, IList<Token>
    {
        public TokenList() : base() {}

        public TokenList(int capacity) : base(capacity) {}
    }
}
