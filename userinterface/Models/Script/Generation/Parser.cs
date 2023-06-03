using System.Diagnostics;

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
        private ParserState State = ParserState.Parameters;

        internal Root RootNode { get; } = new();

        private int CurrentIdx;

        private readonly int MaxIdx;

        private Token CurrentToken;

        private readonly TokenList TokenList;

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

        private void Parse()
        {
            Advance();
            Debug.Assert(CurrentIdx == 1, "We don't need to check for PARAMS_START at runtime.");
            OnStateParameters();
            OnStateVariables();
            OnStateCalculation();
        }

        private void OnStateParameters()
        {
            while (CurrentToken.Symbol != Tokens.Separators.PARAMS_END)
            {
                Assignment(TokenType.Parameter);
            }

            ParserError($"Indeterminate state during {State}!");
        }

        private void OnStateVariables()
        {
            while (CurrentToken.Symbol != Tokens.Separators.CALC_START)
            {
                Assignment(TokenType.Variable);
            }

            ParserError($"Indeterminate state during {State}!");
        }

        private void OnStateCalculation()
        {
            while (CurrentToken.Symbol != Tokens.Separators.CALC_END)
            {
                Statement();
            }

            ParserError($"Indeterminate state during {State}!");
        }

        private void Advance()
        {
            Debug.Assert(CurrentIdx >= 0);
            CurrentToken = TokenList[++CurrentIdx];
        }

        private bool Accept(string symbol)
        {
            if (CurrentToken.Symbol == symbol)
            {
                Advance();
                return true;
            }
            return false;
        }

        private bool Accept(TokenType type)
        {
            if (CurrentToken.Type == type)
            {
                Advance();
                return true;
            }
            return false;
        }

        private bool Expect(string symbol)
        {
            if (Accept(symbol))
            {
                return true;
            }
            ParserError("Unexpected symbol!");
            return false;
        }

        private bool Expect(TokenType type)
        {
            if (Accept(type))
            {
                return true;
            }
            ParserError("Unexpected type of token!");
            return false;
        }

        private void Assignment(TokenType type)
        {
            Expect(type);
            Expect(Tokens.Operators.ASSIGN);
            Expect(TokenType.Number);
            Expect(Tokens.Separators.TERMINATOR);
        }

        private void Statement()
        {
            if (Accept(TokenType.Variable))
            {
                if (Accept(Tokens.Operators.ASSIGN))
                {

                }
                else
                {

                }
            }
        }

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
