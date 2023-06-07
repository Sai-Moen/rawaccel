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
        BranchEnd,

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
        public const int MAX_PARAMETERS     = 8;
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
        public const string BRANCH_END      = "";

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
        // Assignment
        public const string ASSIGN  = ":=";
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
        public const string NOT = "!";
        public const string LT  = "<";
        public const string GT  = ">";
        public const string LE  = "<=";
        public const string GE  = ">=";
        public const string EQ  = "==";
        public const string NE  = "!=";
        public const string AND = "&";
        public const string OR  = "|";

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
            new BaseToken(TokenType.BranchEnd, BRANCH_END),

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

            new BaseToken(TokenType.Comparison, NOT),
            new BaseToken(TokenType.Comparison, LT),
            new BaseToken(TokenType.Comparison, GT),
            new BaseToken(TokenType.Comparison, LE),
            new BaseToken(TokenType.Comparison, GE),
            new BaseToken(TokenType.Comparison, EQ),
            new BaseToken(TokenType.Comparison, NE),
            new BaseToken(TokenType.Comparison, AND),
            new BaseToken(TokenType.Comparison, OR),

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
            int totalLength = ReservedArray.Length + 1;

            ReservedMap = new TokenMap(totalLength);

            foreach(BaseToken baseToken in ReservedArray)
            {
                ReservedMap.Add(baseToken.Symbol, new Token(baseToken));
            }

            string second = SECOND.ToString();
            ReservedMap.Add(second, new(new(TokenType.Undefined, second)));

            Debug.Assert(ReservedMap.Count == totalLength);
        }

        #endregion Constructors

        #region Properties

        public static readonly TokenMap ReservedMap;

        #endregion Properties

        #region Methods

        public static int Precedence(string s) =>
            s switch
            {
                OR => 0,
                AND => 1,

                EQ => 2,
                NE => 2,

                LT => 3,
                GT => 3,
                LE => 3,
                GE => 3,

                ADD => 4,
                SUB => 4,

                MUL => 5,
                DIV => 5,
                MOD => 5,

                EXP => 6,

                NOT => 7,

                _ => throw new ScriptException("Unexpected Precedence call!"),
        };

        public static bool LeftAssociative(string s)
        {
            return s != EXP;
        }

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
