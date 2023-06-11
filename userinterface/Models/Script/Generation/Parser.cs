using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace userinterface.Models.Script.Generation
{
    /// <summary>
    /// Automatically attempts to Parse a list of Tokens.
    /// </summary>
    public class Parser
    {
        #region Fields

        public List<ParameterAssignment> Parameters { get; } = new(Parsing.MAX_PARAMETERS);

        public List<VariableAssignment> Variables { get; } = new(Parsing.MAX_VARIABLES);

        public TokenList TokenCode { get; } = new();

        private readonly List<string> ParameterNames = new(Parsing.MAX_PARAMETERS);

        private readonly List<string> VariableNames = new(Parsing.MAX_VARIABLES);

        private readonly TokenQueue TokenBuffer = new();

        private readonly TokenStack OperatorStack = new();

        private int CurrentIndex;

        private readonly int MaxIndex;

        private Token PreviousToken;

        private Token CurrentToken;

        private readonly TokenList TokenList;

        #endregion Fields

        #region Constructors

        public Parser(TokenList tokenList)
        {
            TokenList = tokenList;
            Debug.Assert(TokenList.Count >= 4, "Tokenizer did not prevent empty script!");

            CurrentToken = TokenList[0];
            Debug.Assert(CmpCurrentTokenType(TokenType.ParameterStart));

            MaxIndex = TokenList.Count - 1;
            Debug.Assert(MaxIndex > 0);

            AdvanceToken();
            Debug.Assert(CurrentIndex == 1, "We don't need to check for PARAMS_START at runtime.");

            PreviousToken = CurrentToken;
            Parse();
        }

        #endregion Constructors

        #region Helpers

        private void Parse()
        {
            while (!CmpCurrentTokenType(TokenType.ParameterEnd))
            {
                DeclParam(TokenType.Identifier);
                DeclParam(TokenType.Assignment);
                DeclParam(TokenType.Number);
                DeclParam(TokenType.Terminator);
            }

            AdvanceToken();
            Debug.Assert(!CmpCurrentTokenType(TokenType.ParameterEnd));

            CoerceAll(ParameterNames, TokenType.Parameter);

            // Parameters MUST be coerced before this!
            while (!CmpCurrentTokenType(TokenType.CalculationStart))
            {
                DeclVar(TokenType.Identifier);
                DeclVar(TokenType.Assignment);
                ExprVar();
                AdvanceToken();
            }

            AdvanceToken();
            Debug.Assert(!CmpCurrentTokenType(TokenType.CalculationStart));

            CoerceAll(VariableNames, TokenType.Variable);

            // Calculation
            while (!CmpCurrentTokenType(TokenType.CalculationEnd))
            {
                Statement();
            }
        }

        private void CoerceAll(in List<string> list, TokenType type)
        {
            for (int i = 0; i <= MaxIndex; i++)
            {
                Token token = TokenList[i];

                if (list.Contains(token.Base.Symbol))
                {
                    TokenList[i] = token with { Base = token.Base with { Type = type } };
                }
            }
        }

        private bool CmpCurrentTokenType(TokenType type)
        {
            return CurrentToken.Base.Type == type;
        }

        private void AdvanceToken()
        {
            if (CurrentIndex == MaxIndex)
            {
                throw new ParserException("End reached unexpectedly!");
            }
            PreviousToken = CurrentToken;
            CurrentToken = TokenList[++CurrentIndex];
        }

        #endregion Helpers

        #region Declarations

        private void DeclParam(TokenType type)
        {
            (int idx, Token token) = DeclExpect(type);

            if (type == TokenType.Identifier)
            {
                token = token with { Base = token.Base with { Type = TokenType.Parameter } };
                TokenList[idx] = token;

                Debug.Assert(ParameterNames.Count <= Parsing.MAX_PARAMETERS);
                if (ParameterNames.Count == Parsing.MAX_PARAMETERS)
                {
                    ParserError($"Too many parameters! (max {Parsing.MAX_PARAMETERS})");
                }

                ParameterNames.Add(token.Base.Symbol);
            }
            
            if (type == TokenType.Terminator)
            {
                Debug.Assert(TokenBuffer.Count == 3);
                Token t = TokenBuffer.Dequeue();
                Token eq = TokenBuffer.Dequeue();
                Token value = TokenBuffer.Dequeue();

                if (eq.Base.Symbol != Tokens.ASSIGN)
                {
                    ParserError($"Expected {Tokens.ASSIGN}");
                }

                Parameters.Add(new(t, value));
            }
            else
            {
                TokenBuffer.Enqueue(token);
            }
        }

        private void DeclVar(TokenType type)
        {
            (int idx, Token token) = DeclExpect(type);

            if (type == TokenType.Identifier)
            {
                token = token with { Base = token.Base with { Type = TokenType.Variable } };
                TokenList[idx] = token;

                Debug.Assert(VariableNames.Count <= Parsing.MAX_VARIABLES);
                if (VariableNames.Count == Parsing.MAX_VARIABLES)
                {
                    ParserError($"Too many variables! (max {Parsing.MAX_VARIABLES})");
                }
                else if (ParameterNames.Contains(token.Base.Symbol))
                {
                    ParserError("Parameter/Variable name conflict!");
                }

                VariableNames.Add(token.Base.Symbol);
            }

            TokenBuffer.Enqueue(token);
        }

        private void ExprVar()
        {
            TokenQueue input = new();

            while (CurrentToken.Base.Type != TokenType.Terminator)
            {
                if (CurrentToken.Base.Type == TokenType.CalculationStart)
                {
                    ParserError("Calculation block reached unexpectedly!");
                }

                input.Enqueue(CurrentToken);
                AdvanceToken();
            }

            Debug.Assert(OperatorStack.Count == 0);

            // Shunting Yard Algorithm (RPN)
            TokenList output = new(input.Count);

            while (input.Count != 0)
            {
                Token token = input.Dequeue();
                switch (token.Base.Type)
                {
                    case TokenType.Number:
                    case TokenType.Parameter:
                    case TokenType.Constant:
                        output.Add(token);
                        continue;
                    case TokenType.Function:
                    case TokenType.Open:
                        OperatorStack.Push(token);
                        continue;
                    case TokenType.Close:
                        OnClose(output);
                        continue;
                    case TokenType.Arithmetic:
                        OnPrecedence(output, token);
                        continue;
                    default:
                        ParserError("Unexpected expression token!");
                        break;
                }
            }

            OnEmptyQueue(output);

            Debug.Assert(TokenBuffer.Count == 2);
            Token t = TokenBuffer.Dequeue();
            Token eq = TokenBuffer.Dequeue();

            if (eq.Base.Symbol != Tokens.ASSIGN)
            {
                ParserError($"Expected {Tokens.ASSIGN}");
            }

            Variables.Add(new(t, output));
        }

        private (int, Token) DeclExpect(TokenType type)
        {
            // Accept has a side-effect, better save these here!
            int idx = CurrentIndex;
            Token token = CurrentToken;

            if (!DeclAccept(type))
            {
                ParserError("Unexpected declaration!");
            }

            return (idx, token);
        }

        private bool DeclAccept(TokenType type)
        {
            if (CmpCurrentTokenType(type))
            {
                AdvanceToken();
                return true;
            }

            return false;
        }

        #endregion Declarations

        #region Calculation

        private void Statement()
        {
            if (Accept(TokenType.Variable) ||
                Accept(TokenType.Input) ||
                Accept(TokenType.Output))
            {
                Token target = PreviousToken;
                Expect(TokenType.Assignment);
                Token assignment = PreviousToken;
                TokenCode.AddRange(Expression(TokenType.Terminator));
                TokenCode.Add(assignment);
                TokenCode.Add(target);
            }
            else if (Expect(TokenType.Branch))
            {
                Token branch = PreviousToken;
                Expect(TokenType.Open);
                TokenCode.AddRange(Expression(TokenType.Close, TokenType.Block));
                TokenCode.Add(branch);
                do Statement();
                while (!Accept(TokenType.Block));
                TokenCode.Add(Tokens.ReservedMap[Tokens.BRANCH_END] with { Line = PreviousToken.Line });
            }
        }

        private TokenList Expression(TokenType end, TokenType after = TokenType.Undefined)
        {
            TokenQueue input = new();

            while (true)
            {
                Token current = CurrentToken;
                TokenType currentType = current.Base.Type;

                if (currentType == TokenType.CalculationEnd)
                {
                    ParserError("Calculation block end reached unexpectedly!");
                }

                AdvanceToken();

                TokenType nextType = CurrentToken.Base.Type;
                bool onlyEnd = after == TokenType.Undefined;
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

            return ShuntingYard(input);
        }

        private TokenList ShuntingYard(TokenQueue input)
        {
            Debug.Assert(OperatorStack.Count == 0);

            // Shunting Yard Algorithm (RPN)
            TokenList output = new(input.Count);

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
                    case TokenType.Function:
                    case TokenType.Open:
                        OperatorStack.Push(token);
                        continue;
                    case TokenType.Close:
                        OnClose(output);
                        continue;
                    case TokenType.Arithmetic:
                    case TokenType.Comparison:
                        OnPrecedence(output, token);
                        continue;
                    default:
                        ParserError("Unexpected expression token!");
                        break;
                }
            }

            OnEmptyQueue(output);

            return output;
        }

        private void OnClose(in TokenList output)
        {
            Token top;

            try
            {
                top = OperatorStack.Peek();
            }
            catch (InvalidOperationException)
            {
                ParserError($"Unexpected {Tokens.CLOSE}");
                return;
            }

            while (top.Base.Type != TokenType.Open)
            {
                output.Add(OperatorStack.Pop());

                if (OperatorStack.Count == 0)
                {
                    ParserError($"No matching {Tokens.OPEN}");
                }

                top = OperatorStack.Peek();
            }

            Debug.Assert(top.Base.Type == TokenType.Open);

            _ = OperatorStack.Pop();
            if (OperatorStack.Count != 0 && OperatorStack.Peek().Base.Type == TokenType.Function)
            {
                output.Add(OperatorStack.Pop());
            }
        }

        private void OnPrecedence(in TokenList output, Token token)
        {
            int pToken = Tokens.Precedence(token.Base.Symbol);
            bool left = Tokens.LeftAssociative(token.Base.Symbol);

            while (true)
            {
                if (OperatorStack.Count == 0)
                {
                    break;
                }

                Token op = OperatorStack.Peek();
                TokenType optype = op.Base.Type;
                if (optype != TokenType.Comparison && optype != TokenType.Arithmetic)
                {
                    break;
                }

                int pOperator = Tokens.Precedence(op.Base.Symbol);
                if (pToken < pOperator || (left && pToken == pOperator))
                {
                    output.Add(OperatorStack.Pop());
                }
                else
                {
                    break;
                }
            }

            OperatorStack.Push(token);
        }

        private void OnEmptyQueue(in TokenList output)
        {
            while (OperatorStack.Count != 0)
            {
                Token token = OperatorStack.Pop();

                if (token.Base.Type == TokenType.Open)
                {
                    ParserError($"No matching {Tokens.CLOSE}");
                }

                output.Add(token);
            }

            if (output.Count == 0)
            {
                ParserError("Empty expression!");
            }
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

        private bool Accept(TokenType type)
        {
            if (type == CurrentToken.Base.Type)
            {
                AdvanceToken();
                return true;
            }

            return false;
        }

        #endregion Calculation

        private void ParserError(string error)
        {
            throw new ParserException(CurrentToken.Line, error);
        }
    }

    public class ParserException : ScriptException
    {
        public ParserException(string message) : base(message) { }

        public ParserException(uint line, string message) : base($"Line {line}: {message}") { }
    }
}
