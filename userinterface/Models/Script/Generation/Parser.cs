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

        private readonly TokenList TokenBuffer = new();

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
            Debug.Assert(CurrentIdx == 0, "We don't need to check for PARAMS_START at runtime.");
            while (++CurrentIdx <= MaxIdx) // Starts at second token as per the Assert ^^^
            {
                CurrentToken = TokenList[CurrentIdx];

                switch (State)
                {
                    case ParserState.Parameters:
                        OnStateParameters();
                        break;
                    case ParserState.Variables:
                        OnStateVariables();
                        break;
                    case ParserState.Calculation:
                        OnStateCalculation();
                        break;
                }
            }
        }

        private void OnStateParameters()
        {
            if (CurrentToken.Symbol == Tokens.Separators.PARAMS_END)
            {
                State = ParserState.Variables;
                return;
            }

            PreCalculationHelper(TokenType.Parameter);
        }

        private void OnStateVariables()
        {
            if (CurrentToken.Symbol == Tokens.Separators.CALC_START)
            {
                State = ParserState.Calculation;
                return;
            }

            PreCalculationHelper(TokenType.Variable);
        }

        private void OnStateCalculation()
        {
            if (CurrentToken.Symbol == Tokens.Separators.CALC_END)
            {
                Debug.Assert(CurrentIdx == MaxIdx);
                return;
            }

            ParserError($"Indeterminate state during {State}!");
        }

        private void PreCalculationHelper(TokenType type)
        {
            if (CurrentToken.Type == type)
            {
                BufferToken();
                return;
            }

            switch (CurrentToken.Type)
            {
                case TokenType.Number:
                    BufferToken();
                    return;
                case TokenType.Operator:
                    if (CurrentToken.Symbol == Tokens.Operators.ASSIGN)
                    {
                        BufferToken();
                        return;
                    }

                    break;
                case TokenType.Separator:
                    if (CurrentToken.Symbol == Tokens.Separators.TERMINATOR)
                    {
                        BufferToken();

                        INode node = ParseNode.Factory(NodeType.Assignment);

                        if (node.TryCreateNodes(GetTokens()))
                        {
                            RootNode.AddExistingNode(node);
                            return;
                        }
                    }

                    break;
            }

            ParserError($"Indeterminate state during {State}!");
        }

        private Token[] GetTokens()
        {
            Token[] tokens = TokenBuffer.ToArray();
            TokenBuffer.Clear();
            return tokens;
        }

        private void BufferToken()
        {
            TokenBuffer.Add(CurrentToken);
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
