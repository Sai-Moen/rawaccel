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

        internal ParserNode RootNode { get; } = new(NodeType.Root, new Token(TokenType.Undefined, 0, ""));

        private ParserNode CurrentNode;

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

            AdvanceToken();
            Debug.Assert(CurrentIdx == 1, "We don't need to check for PARAMS_START at runtime.");

            CurrentNode = RootNode;
            Parse();
        }

        #endregion Constructors

        #region Helpers

        private void Parse()
        {
            // Parameters
            while (CurrentToken.Symbol != Tokens.Separators.PARAMS_END)
            {
                DeclExpect(TokenType.Parameter);
                DeclExpect(Tokens.Operators.ASSIGN);
                DeclExpect(TokenType.Number);
                DeclExpect(Tokens.Separators.TERMINATOR);
            }
            AdvanceToken();
            Debug.Assert(CurrentToken.Symbol != Tokens.Separators.PARAMS_END);

            // Variables
            while (CurrentToken.Symbol != Tokens.Separators.CALC_START)
            {
                DeclExpect(TokenType.Variable);
                DeclExpect(Tokens.Operators.ASSIGN);
                DeclExpect(TokenType.Number | TokenType.Parameter);
                DeclExpect(Tokens.Separators.TERMINATOR);
            }
            AdvanceToken();
            Debug.Assert(CurrentToken.Symbol != Tokens.Separators.CALC_START);

            // Calculation
            while (CurrentToken.Symbol != Tokens.Separators.CALC_END)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool DeclExpect(bool expect)
        {
            if (expect)
            {
                if (CurrentToken.Symbol == Tokens.Separators.TERMINATOR)
                {
                    Token assignee = TokenBuffer.Pop();
                    Token eq = TokenBuffer.Pop();
                    Debug.Assert(eq.Symbol == Tokens.Operators.ASSIGN);
                    ParserNode declaration = new(NodeType.Assignment, eq, RootNode);

                    Token assigned = TokenBuffer.Pop();
                    Debug.Assert(TokenBuffer.Count == 0);

                    NodeType type = assignee.Type == TokenType.Parameter ? NodeType.Identifier : NodeType.Number;
                    declaration.Children.Add(new ParserNode(NodeType.Identifier, assigned, declaration));
                    declaration.Children.Add(new ParserNode(type, assignee, declaration));

                    NodeList list = assigned.Type == TokenType.Parameter ? Parameters : Variables;
                    list.Add(declaration);
                }
                else
                {
                    TokenBuffer.Push(CurrentToken);
                }
                AdvanceToken();
                return true;
            }
            ParserError("Unexpected declaration!");
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool DeclExpect(TokenType type)
        {
            return DeclExpect((CurrentToken.Type & type) != TokenType.Undefined);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool DeclExpect(string symbol)
        {
            return DeclExpect(CurrentToken.Symbol == symbol);
        }

        #endregion Declarations

        #region Recursive Descent

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Advance()
        {
            AdvanceToken();
        }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Expression(uint precedence = 0)
        {
            TokenSet currentSet = Tokens.Operators.ArithmeticSetArray[precedence];

            Debug.Assert(precedence <= Tokens.Operators.MaxPrecedence);
            if (precedence == Tokens.Operators.MaxPrecedence)
            {
                if (Accept(TokenType.Parameter | TokenType.Variable) ||
                    Accept(Tokens.Keywords.BuiltinSet))
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

                do Expression(precedence + 1);
                while (Accept(currentSet));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Statement()
        {
            if (Accept(TokenType.Variable) ||
                Accept(Tokens.Keywords.InOutSet))
            {
                Expect(Tokens.Operators.AssignmentSet);
                Expression();
                Expect(Tokens.Separators.TERMINATOR);
            }
            else
            {
                Expect(Tokens.Keywords.BranchSet);
                Expect(Tokens.Separators.PREC_START);
                Expression();
                Expect(Tokens.Operators.ComparisonSet);
                Expression();
                Expect(Tokens.Separators.PREC_END);
                Expect(Tokens.Separators.BLOCK);
                do Statement();
                while (!Accept(Tokens.Separators.BLOCK));
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
