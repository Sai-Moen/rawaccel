using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace userinterface.Models.Script.Generation
{
    /// <summary>
    /// Automatically attempts to Parse a list of Tokens.
    /// </summary>
    public class Parser
    {
        #region Fields

        private readonly Identifiers ParameterNames = new(Constants.MAX_PARAMETERS);

        private readonly Identifiers VariableNames = new(Constants.MAX_VARIABLES);

        private readonly Queue<Token> TokenBuffer = new();

        private readonly Stack<Token> OperatorStack = new();

        private int CurrentIndex;

        private readonly int MaxIndex;

        private Token PreviousToken;

        private Token CurrentToken;

        private readonly TokenList TokenList;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Parses the input list of Tokens.
        /// </summary>
        /// <param name="tokenList">List of tokens from the script.</param>
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

        #region Properties

        public Parameters Parameters { get; } = new();

        public Variables Variables { get; } = new();

        public TokenList TokenCode { get; } = new();

        #endregion Properties

        #region Helpers

        private void Parse()
        {
            while (!CmpCurrentTokenType(TokenType.ParameterEnd))
            {
                DeclParam();
            }

            AdvanceToken();
            Debug.Assert(!CmpCurrentTokenType(TokenType.ParameterEnd));

            CoerceAll(ParameterNames, TokenType.Parameter);

            // Parameters MUST be coerced before this!
            while (!CmpCurrentTokenType(TokenType.CalculationStart))
            {
                DeclVar();
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

        private void CoerceAll(Identifiers identifiers, TokenType type)
        {
            for (int i = 0; i <= MaxIndex; i++)
            {
                Token token = TokenList[i];

                if (identifiers.Contains(token.Base.Symbol))
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
                ParserError("End reached unexpectedly!");
                return;
            }

            PreviousToken = CurrentToken;
            CurrentToken = TokenList[++CurrentIndex];
        }

        #endregion Helpers

        #region Declarations

        private void DeclParam()
        {
            {
                int previousIndex = DeclExpect(TokenType.Identifier);
                Token token = PreviousToken;
                token = token with { Base = token.Base with { Type = TokenType.Parameter } };
                TokenList[previousIndex] = token;

                Debug.Assert(ParameterNames.Count <= Constants.MAX_PARAMETERS);
                if (ParameterNames.Count == Constants.MAX_PARAMETERS)
                {
                    ParserError($"Too many parameters! (max {Constants.MAX_PARAMETERS})");
                }

                ParameterNames.Add(token.Base.Symbol);
                TokenBuffer.Enqueue(token);
            }

            BufferExpectedToken(TokenType.Assignment);
            BufferExpectedToken(TokenType.Number);

            ParseGuards();

            {
                _ = DeclExpect(TokenType.Terminator);

                Token t = TokenBuffer.Dequeue();
                Token eq = TokenBuffer.Dequeue();
                Token value = TokenBuffer.Dequeue();

                if (eq.Base.Symbol != Tokens.ASSIGN)
                {
                    ParserError($"Expected {Tokens.ASSIGN}");
                }

                Token? minGuard = TokenBuffer.Dequeue().NullifyUndefined();
                Token? min      = TokenBuffer.Dequeue().NullifyUndefined();
                Token? maxGuard = TokenBuffer.Dequeue().NullifyUndefined();
                Token? max      = TokenBuffer.Dequeue().NullifyUndefined();

                Parameters.Add(new(t, value, minGuard, min, maxGuard, max));
            }
        }

        private void ParseGuards(bool firstguard = true)
        {
            void AddDummy(uint amount)
            {
                for (uint i = 0; i < amount; i++)
                {
                    TokenBuffer.Enqueue(Tokens.DUMMY);
                }
            }

            if (DeclAccept(TokenType.Comparison))
            {
                if (PreviousToken.IsGuardMinimum())
                {
                    TokenBuffer.Enqueue(PreviousToken);
                    BufferExpectedToken(TokenType.Number);

                    ParseGuards(false);
                }
                else if (PreviousToken.IsGuardMaximum())
                {
                    if (firstguard)
                    {
                        AddDummy(2);
                    }

                    TokenBuffer.Enqueue(PreviousToken);
                    BufferExpectedToken(TokenType.Number);
                }
                else
                {
                    ParserError("Incorrect comparison for Guard!");
                }
            }
            else if (firstguard)
            {
                AddDummy(4);
            }
            else
            {
                AddDummy(2);
            }
        }

        private void DeclVar()
        {
            {
                int previousIndex = DeclExpect(TokenType.Identifier);
                Token token = PreviousToken;
                token = token with { Base = token.Base with { Type = TokenType.Variable } };
                TokenList[previousIndex] = token;

                Debug.Assert(VariableNames.Count <= Constants.MAX_VARIABLES);
                if (VariableNames.Count == Constants.MAX_VARIABLES)
                {
                    ParserError($"Too many variables! (max {Constants.MAX_VARIABLES})");
                }
                else if (ParameterNames.Contains(token.Base.Symbol))
                {
                    ParserError("Parameter/Variable name conflict!");
                }

                VariableNames.Add(token.Base.Symbol);
                TokenBuffer.Enqueue(token);
            }

            BufferExpectedToken(TokenType.Assignment);

            ExprVar();
            AdvanceToken();
        }

        private void ExprVar()
        {
            Queue<Token> input = new();
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
                    case TokenType.Arithmetic:
                        OnPrecedence(output, token);
                        continue;
                    case TokenType.ArgumentSeparator:
                        continue;
                    case TokenType.Function:
                    case TokenType.Open:
                        OperatorStack.Push(token);
                        continue;
                    case TokenType.Close:
                        OnClose(output);
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

            output.Add(eq);
            output.Add(t);
            Variables.Add(new(t, output));
        }

        private void BufferExpectedToken(TokenType type)
        {
            _ = DeclExpect(type);
            TokenBuffer.Enqueue(PreviousToken);
        }

        private int DeclExpect(TokenType type)
        {
            if (DeclAccept(type))
            {
                return CurrentIndex - 1;
            }

            ParserError("Unexpected declaration!");
            return -1;
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
            Queue<Token> input = new();
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
                bool afterIsNext = after == nextType;
                if (currentType == end && (after == TokenType.Undefined || afterIsNext))
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

        private TokenList ShuntingYard(Queue<Token> input)
        {
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
                    case TokenType.Variable:
                    case TokenType.Input:
                    case TokenType.Output:
                    case TokenType.Constant:
                        output.Add(token);
                        continue;
                    case TokenType.Arithmetic:
                    case TokenType.Comparison:
                        OnPrecedence(output, token);
                        continue;
                    case TokenType.ArgumentSeparator:
                        continue;
                    case TokenType.Function:
                    case TokenType.Open:
                        OperatorStack.Push(token);
                        continue;
                    case TokenType.Close:
                        OnClose(output);
                        continue;
                    default:
                        ParserError("Unexpected expression token!");
                        break;
                }
            }
            OnEmptyQueue(output);
            return output;
        }

        private void OnClose(TokenList output)
        {
            Token top;

            try
            {
                top = OperatorStack.Peek();
            }
            catch (InvalidOperationException)
            {
                ParserError($"Unexpected: {Tokens.CLOSE}");
                return;
            }

            while (top.Base.Type != TokenType.Open)
            {
                output.Add(OperatorStack.Pop());

                if (OperatorStack.Count == 0)
                {
                    ParserError($"No matching: {Tokens.OPEN}");
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

        private void OnPrecedence(TokenList output, Token token)
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

        private void OnEmptyQueue(TokenList output)
        {
            while (OperatorStack.Count != 0)
            {
                Token token = OperatorStack.Pop();

                if (token.Base.Type == TokenType.Open)
                {
                    ParserError($"No matching: {Tokens.CLOSE}");
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
            throw new ParserException(error, CurrentToken.Line);
        }
    }

    /// <summary>
    /// Exception for parsing-related errors.
    /// </summary>
    public sealed class ParserException : GenerationException
    {
        public ParserException(string message) : base(message) { }

        public ParserException(string message, uint line) : base(message, line) { }
    }
}
