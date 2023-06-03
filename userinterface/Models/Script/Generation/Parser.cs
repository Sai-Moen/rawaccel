using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace userinterface.Models.Script.Generation
{
    /// <summary>
    /// Phase of the Parser, similar to the tokenizer,
    /// but without the Comments Section, as there are no Comment Tokens.
    /// </summary>
    internal enum ParserState
    {
        Parameters,
        Variables,
        Calculation,
    }

    /// <summary>
    /// Automatically attempts to Parse a list of Tokens into a Syntax Tree of Nodes.
    /// </summary>
    internal class Parser
    {
        #region Fields

        private ParserState State = ParserState.Parameters;

        internal NodeList Parameters { get; } = new(8);

        internal NodeList Variables { get; } = new();

        internal Root RootNode { get; } = new();

        private readonly TokenStack TokenBuffer = new();

        private int CurrentIdx;

        private readonly int MaxIdx;

        private Token CurrentToken;

        private readonly TokenList TokenList;

        #endregion Fields

        #region Constructors

        internal Parser(TokenList tokenList)
        {
            TokenList = tokenList;
            Debug.Assert(TokenList.Count >= 4, "Tokenizer did not prevent empty script!");

            CurrentToken = TokenList[0];
            Debug.Assert(CurrentToken.Symbol == Tokens.Separators.PARAMS_START);

            MaxIdx = TokenList.Count - 1;
            Debug.Assert(MaxIdx > 0);

            Parse();
        }

        #endregion Constructors

        #region Methods

        private void Parse()
        {
            CurrentToken = TokenList[++CurrentIdx];
            Debug.Assert(CurrentIdx == 1, "We don't need to check for PARAMS_START at runtime.");
            OnStateParameters();
            OnStateVariables();
            OnStateCalculation();
        }

        private void OnStateParameters()
        {
            while (CurrentToken.Symbol != Tokens.Separators.PARAMS_END)
            {
                Expect(TokenType.Parameter);
                Expect(Tokens.Operators.ASSIGN);
                Expect(TokenType.Number);
                Expect(Tokens.Separators.TERMINATOR);
            }

            State = ParserState.Variables;
        }

        private void OnStateVariables()
        {
            while (CurrentToken.Symbol != Tokens.Separators.CALC_START)
            {
                Expect(TokenType.Variable);
                Expect(Tokens.Operators.ASSIGN);
                Expect(TokenType.Number | TokenType.Parameter);
                Expect(Tokens.Separators.TERMINATOR);
            }

            State = ParserState.Calculation;
        }

        private void OnStateCalculation()
        {
            while (CurrentToken.Symbol != Tokens.Separators.CALC_END)
            {
                Statement();
            }

            ParserError($"Indeterminate state during {State}!");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Advance()
        {
            switch (State)
            {
                case ParserState.Parameters:
                    break;
                case ParserState.Variables:
                    break;
                case ParserState.Calculation:
                    break;
            }

            CurrentToken = TokenList[++CurrentIdx];
        }

        #endregion Methods

        #region Recursive Descent

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Accept(bool accept)
        {
            if (accept)
            {
                Advance();
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Accept(TokenType type)
        {
            return Accept((CurrentToken.Type & type) != TokenType.Undefined);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Accept(string symbol)
        {
            return Accept(CurrentToken.Symbol == symbol);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Accept(TokenSet set)
        {
            return Accept(set.Contains(CurrentToken.Symbol));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Expect(TokenType type)
        {
            if (Accept(type))
            {
                return true;
            }
            ParserError("Unexpected type of token!");
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Expect(string symbol)
        {
            if (Accept(symbol))
            {
                return true;
            }
            ParserError("Unexpected symbol!");
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Expect(TokenSet set)
        {
            if (Accept(set))
            {
                return true;
            }
            ParserError("Unexpected token!");
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private void Expression(uint precedence = 0)
        {
            TokenSet currentSet = Tokens.Operators.ArithmeticSetArray[precedence];

            Debug.Assert(precedence <= Tokens.Operators.MaxPrecedence);
            if (precedence == Tokens.Operators.MaxPrecedence)
            {
                if (Accept(Tokens.Keywords.PredefinedSet) ||
                    Accept(TokenType.Parameter | TokenType.Variable))
                {
                    if (Accept(currentSet))
                    {
                        Expression(precedence); // Tail Call
                    }
                }
                else
                {
                    if (Accept(Tokens.Functions.FunctionsSet)) {}
                    Expect(Tokens.Separators.PREC_START);
                    Expression();
                    Expect(Tokens.Separators.PREC_END);
                }
            }
            else
            {
                if (precedence == 0 && Accept(currentSet)) {}
                do { Expression(precedence + 1); } while (Accept(currentSet));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private void Statement()
        {
            if (Accept(TokenType.Variable))
            {
                if (Accept(Tokens.Operators.ASSIGN) || Accept(Tokens.Operators.InlineSet))
                {
                    Expression();
                    Expect(Tokens.Separators.TERMINATOR);
                }
            }
            else if (Accept(Tokens.Keywords.BRANCH_IF) || Accept(Tokens.Keywords.BRANCH_WHILE))
            {
                Expect(Tokens.Separators.PREC_START);
                Expression();
                Expect(Tokens.Operators.ComparisonSet);
                Expression();
                Expect(Tokens.Separators.PREC_END);
                Expect(Tokens.Separators.BLOCK);
                do { Statement(); } while (!Accept(Tokens.Separators.BLOCK));
            }
            else
            {
                ParserError("Could not parse statement!");
            }
        }

        #endregion Recursive Descent

        private void ParserError(string error)
        {
            throw new ParserException(CurrentToken.Line, error);
        }
    }

    public class ParserException : TranspilerException
    {
        public ParserException(string message) : base(message) {}

        public ParserException(uint line, string message) : base($"Line {line}: {message}") {}
    }
}
