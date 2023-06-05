using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace userinterface.Models.Script.Generation
{
    /// <summary>
    /// Automatically attempts to Parse a list of Tokens into a Syntax Tree of Nodes.
    /// </summary>
    internal class Parser
    {
        #region Fields

        internal NodeList Parameters { get; } = new(8);

        internal NodeList Variables { get; } = new();

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
            Debug.Assert(CurrentToken.Base.Symbol == Tokens.PARAMS_START);

            MaxIdx = TokenList.Count - 1;
            Debug.Assert(MaxIdx > 0);

            AdvanceToken();
            Debug.Assert(CurrentIdx == 1, "We don't need to check for PARAMS_START at runtime.");

            CheckTokens();

            Parse();
        }

        #endregion Constructors

        #region Helpers

        private void CheckTokens()
        {
        }

        private void Parse()
        {
            // Parameters
            while (CurrentToken.Base.Symbol != Tokens.PARAMS_END)
            {
                //DeclExpect(TokenType.Parameter);
                //DeclExpect(Tokens.ASSIGN);
                //DeclExpect(TokenType.Number);
                //DeclExpect(Tokens.TERMINATOR);
            }
            AdvanceToken();
            Debug.Assert(CurrentToken.Base.Symbol != Tokens.PARAMS_END);

            // Variables
            while (CurrentToken.Base.Symbol != Tokens.CALC_START)
            {
                //DeclExpect(TokenType.Variable);
                //DeclExpect(Tokens.ASSIGN);
                //DeclExpect();
                //DeclExpect(Tokens.TERMINATOR);
            }
            AdvanceToken();
            Debug.Assert(CurrentToken.Base.Symbol != Tokens.CALC_START);

            // Calculation
            while (CurrentToken.Base.Symbol != Tokens.CALC_END)
            {
                Statement();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AdvanceToken()
        {
            CurrentToken = TokenList[++CurrentIdx];
        }

        #endregion Helpers

        #region Declarations



        #endregion Declarations

        #region Recursive Descent

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Advance()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Accept(TokenType type)
        {
            if (CurrentToken.Base.Type == type)
            {
                Advance();
                AdvanceToken();
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Expect(TokenType type)
        {
            if (Accept(type))
            {
                return true;
            }
            ParserError("Unexpected token!");
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Expression(uint precedence = 0)
        {
            const uint maxPrecedence = 2;

            TokenType arithmetic = Tokens.Arithmetic[precedence];

            if (precedence == maxPrecedence)
            {
                if (Accept(TokenType.Parameter) ||
                    Accept(TokenType.Variable) ||
                    Accept(TokenType.Input) ||
                    Accept(TokenType.Output) ||
                    Accept(TokenType.Constant))
                {
                    if (Accept(arithmetic))
                    {
                        Expression(precedence); // Tail Call
                    }
                }
                else
                {
                    if (Accept(TokenType.Function)) {}

                    Expect(TokenType.Open);
                    Expression();
                    Expect(TokenType.Close);
                }
            }
            else
            {
                if (precedence == 0 && Accept(arithmetic)) {}

                do Expression(precedence + 1);
                while (Accept(arithmetic));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Statement()
        {
            if (Accept(TokenType.Variable) ||
                Accept(TokenType.Input) ||
                Accept(TokenType.Output))
            {
                Expect(TokenType.Assignment);
                Expression();
                Expect(TokenType.Terminator);
            }
            else
            {
                Expect(TokenType.Branch);
                Expect(TokenType.Open);
                Expression();
                Expect(TokenType.Comparison);
                Expression();
                Expect(TokenType.Close);
                Expect(TokenType.Block);
                do Statement();
                while (!Accept(TokenType.Block));
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
