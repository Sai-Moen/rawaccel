using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace userinterface.Models.Script.Generation
{
    /// <summary>
    /// Automatically attempts to Parse a list of Tokens.
    /// </summary>
    internal class Parser
    {
        #region Fields

        internal IList<ParameterAssignment> Parameters { get; } = new List<ParameterAssignment>(Tokens.MAX_PARAMETERS);

        internal IList<VariableAssignment> Variables { get; } = new List<VariableAssignment>();

        internal IList<ParserNode> Nodes { get; } = new List<ParserNode>();

        private readonly IList<string> ParameterNames = new List<string>();

        private readonly ISet<string> VariableNames = new HashSet<string>();

        private readonly TokenStack TokenBuffer = new();

        private int CurrentIndex;

        private readonly int MaxIndex;

        private Token CurrentToken;

        private readonly TokenList TokenList;

        #endregion Fields

        #region Constructors

        internal Parser(TokenList tokenList)
        {
            TokenList = tokenList;
            Debug.Assert(TokenList.Count >= 4, "Tokenizer did not prevent empty script!");

            CurrentToken = TokenList[0];
            Debug.Assert(CmpCurrentTokenType(TokenType.ParameterStart));

            MaxIndex = TokenList.Count - 1;
            Debug.Assert(MaxIndex > 0);

            AdvanceToken();
            Debug.Assert(CurrentIndex == 1, "We don't need to check for PARAMS_START at runtime.");

            Parse();
        }

        #endregion Constructors

        #region Helpers

        private void Parse()
        {
            // Parameters
            while (!CmpCurrentTokenType(TokenType.ParameterEnd))
            {
                DeclExpect(TokenType.Parameter, true);
                DeclExpect(TokenType.Assignment);
                DeclExpect(TokenType.Number);
                DeclExpect(TokenType.Terminator);
            }

            AdvanceToken();
            Debug.Assert(!CmpCurrentTokenType(TokenType.ParameterEnd));
            for (int i = 0; i <= MaxIndex; i++)
            {
                Token token = TokenList[i];

                if (ParameterNames.Contains(token.Base.Symbol))
                {
                    TokenList[i] = token with { Base = token.Base with { Type = TokenType.Parameter } };
                }
            }

            // Variables
            while (!CmpCurrentTokenType(TokenType.CalculationStart))
            {
                DeclExpect(TokenType.Variable, true);
                DeclExpect(TokenType.Assignment);
                if (DeclAccept(TokenType.Number, true)) {}
                else DeclExpect(TokenType.Parameter);
                DeclExpect(TokenType.Terminator);
            }

            AdvanceToken();
            Debug.Assert(!CmpCurrentTokenType(TokenType.CalculationStart));
            for (int i = 0; i <= MaxIndex; i++)
            {
                Token token = TokenList[i];

                if (VariableNames.Contains(token.Base.Symbol))
                {
                    TokenList[i] = token with { Base = token.Base with { Type = TokenType.Variable } };
                }
            }

            // Calculation
            while (!CmpCurrentTokenType(TokenType.CalculationEnd))
            {
                Statement();
            }
        }

        private bool CmpCurrentTokenType(TokenType type)
        {
            return CurrentToken.Base.Type == type;
        }

        private void Advance(TokenType type)
        {

        }

        private void AdvanceToken()
        {
            CurrentToken = TokenList[++CurrentIndex];
        }

        #endregion Helpers

        #region Declarations

        private bool DeclAccept(TokenType type, bool push = false)
        {
            if (CmpCurrentTokenType(type))
            {
                if (push)
                {
                    TokenBuffer.Push(CurrentToken);
                }
                AdvanceToken();
                return true;
            }

            return false;
        }

        private void DeclExpect(TokenType type, bool coerce = false)
        {
            // Accept has side-effect on "Current...", better save these here!
            int idx = CurrentIndex;
            Token token = CurrentToken;

            if (coerce)
            {
                token = token with { Base = token.Base with { Type = type } };
                TokenList[idx] = token;

                if (type == TokenType.Parameter)
                {
                    Debug.Assert(ParameterNames.Count <= Tokens.MAX_PARAMETERS);
                    if (ParameterNames.Count == Tokens.MAX_PARAMETERS)
                    {
                        ParserError($"Too many parameters! (max {Tokens.MAX_PARAMETERS})");
                    }

                    ParameterNames.Add(token.Base.Symbol);
                }
                else
                {
                    if (ParameterNames.Contains(token.Base.Symbol))
                    {
                        ParserError("Parameter/Variable name conflict!");
                    }

                    VariableNames.Add(token.Base.Symbol);
                }

                CurrentToken = token; // Side effect
                if (DeclAccept(type, true))
                {
                    TokenBuffer.Push(token);
                    return;
                }

                ParserError("Unexpected coercion!");
            }

            if (!DeclAccept(type))
            {
                ParserError("Unexpected declaration!");
            }
            else if (type == TokenType.Terminator)
            {
                Debug.Assert(TokenBuffer.Count == 3);
                Token value = TokenBuffer.Pop();
                Token eq = TokenBuffer.Pop();
                Token t = TokenBuffer.Pop();

                if (eq.Base.Symbol != Tokens.ASSIGN)
                {
                    ParserError($"Expected {Tokens.ASSIGN}");
                }

                TokenType currentType = t.Base.Type;
                Debug.Assert(currentType == TokenType.Parameter || currentType == TokenType.Variable);

                if (currentType == TokenType.Parameter)
                {
                    Parameters.Add(new(t, value));
                }
                else
                {
                    Variables.Add(new(t, value));
                }
            }
            else
            {
                TokenBuffer.Push(token);
            }
        }

        #endregion Declarations

        #region Recursive Descent

        private bool Accept(TokenType type)
        {
            if (type == CurrentToken.Base.Type)
            {
                Advance(type);
                AdvanceToken();
                return true;
            }

            return false;
        }

        private bool Expect(TokenType type)
        {
            if (Accept(type))
            {
                return true;
            }

            ParserError("Unexpected token!");
            return false;
        }

        private TokenList ShuntingYard(in TokenQueue input)
        {
            // Shunting yard algorithm (to Reverse Polish Notation)
            TokenList output = new(input.Count);

            TokenStack opstack = new();

            while (input.Count != 0)
            {
                Token token = input.Dequeue();
                switch (token.Base.Type)
                {
                    case TokenType.Parameter:
                    case TokenType.Variable:
                    case TokenType.Input:
                    case TokenType.Output:
                    case TokenType.Constant:
                        output.Add(token);
                        continue;
                    case TokenType.Open:
                        opstack.Push(token);
                        continue;
                    case TokenType.Close:
                        Token top;

                        try
                        {
                            top = opstack.Peek();
                        }
                        catch (InvalidOperationException)
                        {
                            ParserError($"Unexpected {Tokens.CLOSE}");
                            break;
                        }

                        while (top.Base.Type != TokenType.Open)
                        {
                            output.Add(opstack.Pop());

                            if (opstack.Count == 0)
                            {
                                ParserError($"No matching {Tokens.OPEN}");
                            }

                            top = opstack.Peek();
                        }

                        Debug.Assert(top.Base.Type == TokenType.Open);

                        _ = opstack.Pop();
                        if (opstack.Count != 0 && opstack.Peek().Base.Type == TokenType.Function)
                        {
                            output.Add(opstack.Pop());
                        }

                        continue;
                    case TokenType.Arithmetic:
                        int precedenceT = Tokens.Precedence(token.Base.Symbol);
                        bool left = Tokens.LeftAssociative(token.Base.Symbol);

                        while (true)
                        {
                            if (opstack.Count == 0)
                            {
                                break;
                            }

                            Token op = opstack.Peek();
                            int precedenceO = Tokens.Precedence(op.Base.Symbol);
                            if (op.Base.Type == TokenType.Arithmetic &&
                                (precedenceT < precedenceO ||
                                (precedenceT == precedenceO && left)))
                            {
                                output.Add(opstack.Pop());
                            }
                            else
                            {
                                break;
                            }
                        }

                        opstack.Push(token);

                        continue;
                    case TokenType.Function:
                        opstack.Push(token);
                        continue;
                    default:
                        ParserError("Unexpected expression token!");
                        break;
                }
            }

            while (opstack.Count != 0)
            {
                Token token = opstack.Pop();

                if (token.Base.Type == TokenType.Open)
                {
                    ParserError($"No matching {Tokens.CLOSE}");
                }

                output.Add(token);
            }

            return output;
        }

        private void Expression(TokenType end, TokenType after = TokenType.Undefined)
        {
            bool onlyEnd = after == TokenType.Undefined;

            TokenQueue input = new();

            while (true)
            {
                Token current = CurrentToken;
                TokenType currentType = current.Base.Type;

                if (currentType == TokenType.CalculationEnd)
                {
                    ParserError("Expression end reached unexpectedly");
                    return;
                }

                AdvanceToken();

                TokenType nextType = CurrentToken.Base.Type;
                bool afterIsNext = after == nextType;

                if (currentType == end && (onlyEnd || afterIsNext))
                {
                    if (afterIsNext)
                    {
                        AdvanceToken();
                    }
                    break;
                }

                input.Enqueue(current);
            }

            TokenList output = ShuntingYard(input);

            // Turn into AST
        }

        private void Statement()
        {
            if (Accept(TokenType.Variable) ||
                Accept(TokenType.Input) ||
                Accept(TokenType.Output))
            {
                Expect(TokenType.Assignment);
                Expression(TokenType.Terminator);
            }
            else if (Expect(TokenType.Branch))
            {
                Expect(TokenType.Open);
                Expression(TokenType.Comparison);
                Expression(TokenType.Close, TokenType.Block);
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
