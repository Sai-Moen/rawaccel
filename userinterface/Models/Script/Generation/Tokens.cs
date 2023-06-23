﻿using System.Collections.Generic;
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
    /// <param name="Line">The line in the file where this came from.</param>
    public record Token(BaseToken Base, uint Line = 0);

    /// <summary>
    /// Defines all reserved kinds of Tokens.
    /// </summary>
    public static class Tokens
    {
        #region Constants

        // Keywords
        // Calculation IO
        public const string INPUT           = "x";
        public const string OUTPUT          = "y";

        // Unary Minus Hack
        public const string ZERO            = "zero";

        // Constants
        public const string CONST_E         = "e";
        public const string CONST_PI        = "pi";
        public const string CONST_TAU       = "tau";

        // Branching
        public const string BRANCH_IF       = "if";
        public const string BRANCH_WHILE    = "while";
        public const string BRANCH_END      = ""; // Using an empty string to avoid tokenization, be careful...

        // Separators
        // Delimiters
        public const string SPACE           = " ";
        public const string UNDER           = "_";
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
        public const string SECOND  = "=";     // if there is a second character, it should be this!
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
            // Special untyped 'characters' that show up sometimes
            new(TokenType.Undefined, SECOND),
            new(TokenType.Undefined, UNDER),

            new(TokenType.Input, INPUT),
            new(TokenType.Output, OUTPUT),

            new(TokenType.Constant, ZERO),
            new(TokenType.Constant, CONST_E),
            new(TokenType.Constant, CONST_PI),
            new(TokenType.Constant, CONST_TAU),

            new(TokenType.Branch, BRANCH_IF),
            new(TokenType.Branch, BRANCH_WHILE),
            new(TokenType.BranchEnd, BRANCH_END),

            new(TokenType.Number, FPOINT),
            new(TokenType.Terminator, TERMINATOR),
            new(TokenType.Block, BLOCK),

            new(TokenType.Open, OPEN),
            new(TokenType.Close, CLOSE),

            new(TokenType.ParameterStart, PARAMS_START),
            new(TokenType.ParameterEnd, PARAMS_END),

            new(TokenType.CalculationStart, CALC_START),
            new(TokenType.CalculationEnd, CALC_END),

            new(TokenType.Assignment, ASSIGN),
            new(TokenType.Assignment, IADD),
            new(TokenType.Assignment, ISUB),
            new(TokenType.Assignment, IMUL),
            new(TokenType.Assignment, IDIV),
            new(TokenType.Assignment, IMOD),
            new(TokenType.Assignment, IEXP),

            new(TokenType.Arithmetic, ADD),
            new(TokenType.Arithmetic, SUB),
            new(TokenType.Arithmetic, MUL),
            new(TokenType.Arithmetic, DIV),
            new(TokenType.Arithmetic, MOD),
            new(TokenType.Arithmetic, EXP),

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
            new(TokenType.Function, SQRT),
            new(TokenType.Function, CBRT),

            new(TokenType.Function, ROUND),
            new(TokenType.Function, TRUNC),
            new(TokenType.Function, CEIL),
            new(TokenType.Function, FLOOR),

            new(TokenType.Function, LOG),
            new(TokenType.Function, LOG2),
            new(TokenType.Function, LOG10),

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
        };

        #endregion Fields

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

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Maps all of the reserved Tokens to a string representation.
        /// </summary>
        public static TokenMap ReservedMap { get; }

        #endregion Properties

        #region Methods

        public static bool IsLoop(this Token token)
        {
            return token.Base.Symbol == BRANCH_WHILE;
        }

        public static string SpaceReplace(string s)
        {
            return s.Replace(UNDER, SPACE);
        }

        public static bool LeftAssociative(string s)
        {
            return s != EXP;
        }

        public static int Precedence(string s)
        {
            return s switch
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
        }

        #endregion Methods
    }

    public class TokenMap : Dictionary<string, Token>, IDictionary<string, Token>
    {
        public TokenMap(int capacity) : base(capacity) { }
    }

    public class TokenList : List<Token>, IList<Token>
    {
        public TokenList() : base() { }

        public TokenList(int capacity) : base(capacity) { }
    }
}
