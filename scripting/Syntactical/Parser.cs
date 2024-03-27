using scripting.Common;
using scripting.Lexical;
using scripting.Script;

namespace scripting.Syntactical;

/// <summary>
/// Can parse a list of Tokens.
/// </summary>
public class Parser : IParser
{
    #region Fields

    private readonly ISet<string> parameterNames = new HashSet<string>(Constants.MAX_PARAMETERS);
    private readonly ISet<string> variableNames = new HashSet<string>(Constants.MAX_VARIABLES);

    private readonly Queue<Token> tokenBuffer = new();
    private readonly Stack<Token> operatorStack = new();

    private int currentIndex;
    private readonly int maxIndex;

    private Token previousToken;
    private Token currentToken;
    private readonly IList<Token> lexicalTokens;

    private readonly string description;
    private readonly Parameters parameters = new();
    private readonly Variables variables = new();
    private readonly List<Token> syntacticTokens = new();

    #endregion

    #region Constructors

    /// <summary>
    /// Parses the input list of Tokens.
    /// </summary>
    /// <param name="tokens">List of tokens from the script.</param>
    public Parser(LexingResult tokens)
    {
        description = tokens.Comments.Trim();
        lexicalTokens = tokens.Tokens;
        Debug.Assert(lexicalTokens.Count >= 4, "Tokenizer did not prevent empty script!");

        currentToken = lexicalTokens[0];
        Debug.Assert(currentToken.Type == TokenType.ParameterStart);

        maxIndex = lexicalTokens.Count - 1;
        Debug.Assert(maxIndex > 0);

        AdvanceToken();
        Debug.Assert(currentIndex == 1, "We don't need to check for PARAMS_START at runtime.");

        previousToken = currentToken;
    }

    #endregion

    #region Methods

    public ParsingResult Parse()
    {
        ParseTokens();
        return new(description, parameters, variables, syntacticTokens);
    }

    private void ParseTokens()
    {
        // Parameters
        while (currentToken.Type != TokenType.ParameterEnd)
        {
            DeclareParameter();
        }
        CoerceAll(parameterNames, TokenType.Parameter);

        AdvanceToken();
        Debug.Assert(currentToken.Type != TokenType.ParameterEnd);

        // Variables
        while (currentToken.Type != TokenType.CalculationStart)
        {
            DeclareVariable();
        }
        CoerceAll(variableNames, TokenType.Variable);

        AdvanceToken();
        Debug.Assert(currentToken.Type != TokenType.CalculationStart);

        // Calculation
        while (currentToken.Type != TokenType.CalculationEnd)
        {
            Statement();
        }
    }

    private void CoerceAll(ISet<string> identifiers, TokenType type)
    {
        for (int i = 0; i <= maxIndex; i++)
        {
            Token token = lexicalTokens[i];
            if (identifiers.Contains(token.Symbol))
            {
                lexicalTokens[i] = Tokens.TokenWithType(token, type);
            }
        }
    }

    #endregion

    #region Parameter Declarations

    private void DeclareParameter()
    {
        int previousIndex = DeclExpect(TokenType.Identifier);
        Token token = Tokens.TokenWithType(previousToken, TokenType.Parameter);
        lexicalTokens[previousIndex] = token;

        string symbol = token.Symbol;

        Debug.Assert(parameterNames.Count <= Constants.MAX_PARAMETERS);
        if (parameterNames.Count == Constants.MAX_PARAMETERS)
        {
            ParserError($"Too many parameters! (max {Constants.MAX_PARAMETERS})");
        }
        else if (parameterNames.Contains(symbol))
        {
            ParserError("Parameter name already exists!");
        }

        parameterNames.Add(symbol);

        tokenBuffer.Enqueue(token);
        BufferExpectedToken(TokenType.Assignment);
        if (DeclAccept(TokenType.Bool))
        {
            TerminateParameter(out Token nameB, out Token valueB);
            parameters.Add(new(nameB, valueB, null, null, null, null));
            return;
        }

        BufferExpectedToken(TokenType.Number);
        ParseGuards();
        TerminateParameter(out Token name, out Token value);
        
        Token? DequeueMaybeDummy() => tokenBuffer.Dequeue().NullifyUndefined();

        Token? minGuard = DequeueMaybeDummy();
        Token? min      = DequeueMaybeDummy();
        Token? maxGuard = DequeueMaybeDummy();
        Token? max      = DequeueMaybeDummy();

        parameters.Add(new(name, value, minGuard, min, maxGuard, max));
    }

    private void TerminateParameter(out Token name, out Token value)
    {
        _ = DeclExpect(TokenType.Terminator);

        name = tokenBuffer.Dequeue();
        Token eq = tokenBuffer.Dequeue();
        value = tokenBuffer.Dequeue();

        if (eq.Symbol != Tokens.ASSIGN)
        {
            ParserError($"Expected {Tokens.ASSIGN}");
        }
    }

    private void ParseGuards(bool firstguard = true)
    {
        void AddDummy(uint amount)
        {
            for (uint i = 0; i < amount; i++)
            {
                tokenBuffer.Enqueue(Tokens.DUMMY);
            }
        }

        if (DeclAccept(TokenType.Comparison))
        {
            if (previousToken.IsGuardMinimum())
            {
                tokenBuffer.Enqueue(previousToken);
                BufferExpectedToken(TokenType.Number);

                ParseGuards(false);
            }
            else if (previousToken.IsGuardMaximum())
            {
                if (firstguard)
                {
                    AddDummy(2);
                }

                tokenBuffer.Enqueue(previousToken);
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

    #endregion

    #region Variable Declarations

    private void DeclareVariable()
    {
        int previousIndex = DeclExpect(TokenType.Identifier);
        Token token = Tokens.TokenWithType(previousToken, TokenType.Variable);
        lexicalTokens[previousIndex] = token;

        string symbol = token.Symbol;

        Debug.Assert(variableNames.Count <= Constants.MAX_VARIABLES);
        if (variableNames.Count == Constants.MAX_VARIABLES)
        {
            ParserError($"Too many variables! (max {Constants.MAX_VARIABLES})");
        }
        else if (parameterNames.Contains(symbol))
        {
            ParserError("Parameter/Variable name conflict!");
        }
        else if (variableNames.Contains(symbol))
        {
            ParserError("Variable name already exists!");
        }

        tokenBuffer.Enqueue(token);
        BufferExpectedToken(TokenType.Assignment);
        ExprVar();
        
        // this must be done after ExprVar, due to name resolution
        variableNames.Add(symbol);
    }

    private void ExprVar()
    {
        Queue<Token> input = new();
        while (currentToken.Type != TokenType.Terminator)
        {
            if (currentToken.Type == TokenType.CalculationStart)
            {
                ParserError("Calculation block reached unexpectedly!");
            }

            input.Enqueue(currentToken);
            AdvanceToken();
        }
        AdvanceToken();

        Debug.Assert(operatorStack.Count == 0);

        // Shunting Yard Algorithm (RPN)
        IList<Token> output = new List<Token>(input.Count);

        Token? prev = null;
        foreach (Token token in input)
        {
            switch (token.Type)
            {
                case TokenType.Identifier:
                    if (!variableNames.Contains(token.Symbol))
                    {
                        ParserError("Could not resolve variable name!");
                    }

                    output.Add(Tokens.TokenWithType(token, TokenType.Variable));
                    break;
                case TokenType.Number:
                case TokenType.Parameter:
                case TokenType.Constant:
                    output.Add(token);
                    break;
                case TokenType.Arithmetic:
                    CheckUnary(output, prev, token.Line);
                    OnPrecedence(output, token);
                    break;
                case TokenType.ArgumentSeparator:
                    break;
                case TokenType.Function:
                case TokenType.Open:
                    operatorStack.Push(token);
                    break;
                case TokenType.Close:
                    OnClose(output);
                    break;
                default:
                    ParserError("Unexpected expression token!");
                    break; // unreachable
            }
            prev = token;
        }
        OnEmptyQueue(output);

        Debug.Assert(tokenBuffer.Count == 2);

        Token t = tokenBuffer.Dequeue();
        Token eq = tokenBuffer.Dequeue();

        if (eq.Symbol != Tokens.ASSIGN)
        {
            ParserError($"Expected {Tokens.ASSIGN}");
        }

        output.Add(eq);
        output.Add(t);
        variables.Add(new(t.Symbol, output));
    }

    #endregion

    #region Declaration Helpers

    private void BufferExpectedToken(TokenType type)
    {
        _ = DeclExpect(type);
        tokenBuffer.Enqueue(previousToken);
    }

    private int DeclExpect(TokenType type)
    {
        if (DeclAccept(type))
        {
            return currentIndex - 1;
        }

        ParserError("Unexpected declaration!");
        return -1; // unreachable
    }

    private bool DeclAccept(TokenType type)
    {
        bool success = currentToken.Type == type;
        if (success) AdvanceToken();
        return success;
    }

    #endregion

    #region Calculation

    private void Statement()
    {
        if (
            Accept(TokenType.Variable) ||
            Accept(TokenType.Input) ||
            Accept(TokenType.Output))
        {
            Token target = previousToken;
            Expect(TokenType.Assignment);

            Token assignment = previousToken;
            syntacticTokens.AddRange(Expression(TokenType.Terminator));
            syntacticTokens.Add(assignment);
            syntacticTokens.Add(target);
        }
        else if (Accept(TokenType.Return))
        {
            Expect(TokenType.Terminator);
        }
        else if (Expect(TokenType.Branch))
        {
            Token branch = previousToken;
            Expect(TokenType.Open);
            syntacticTokens.AddRange(Expression(TokenType.Close, TokenType.Block));
            syntacticTokens.Add(branch);

            do Statement();
            while (!Accept(TokenType.Block));
            syntacticTokens.Add(Tokens.GetReserved(Tokens.BRANCH_END, previousToken.Line));
        }
    }

    private IList<Token> Expression(TokenType end, TokenType after = TokenType.Undefined)
    {
        Queue<Token> input = new();
        while (true)
        {
            Token current = currentToken;
            TokenType currentType = current.Type;

            if (currentType == TokenType.CalculationEnd)
            {
                ParserError("Calculation block end reached unexpectedly!");
            }

            AdvanceToken();
            TokenType nextType = currentToken.Type;
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
        Debug.Assert(operatorStack.Count == 0);

        // Shunting Yard Algorithm (RPN)
        IList<Token> output = new List<Token>(input.Count);

        Token? prev = null;
        foreach (Token token in input)
        {
            switch (token.Type)
            {
                case TokenType.Number:
                case TokenType.Parameter:
                case TokenType.Variable:
                case TokenType.Input:
                case TokenType.Output:
                case TokenType.Constant:
                    output.Add(token);
                    break;
                case TokenType.Arithmetic:
                    CheckUnary(output, prev, token.Line);
                    OnPrecedence(output, token);
                    break;
                case TokenType.Comparison:
                    OnPrecedence(output, token);
                    break;
                case TokenType.ArgumentSeparator:
                    break;
                case TokenType.Function:
                case TokenType.Open:
                    operatorStack.Push(token);
                    break;
                case TokenType.Close:
                    OnClose(output);
                    break;
                default:
                    ParserError("Unexpected expression token!");
                    break; // unreachable
            }
            prev = token;
        }
        OnEmptyQueue(output);
        return output;
    }

    private void OnClose(IList<Token> output)
    {
        Token top;
        if (!operatorStack.TryPeek(out top!))
        {
            ParserError($"Unexpected: {Tokens.CLOSE}");
            return;
        }

        while (top.Type != TokenType.Open)
        {
            output.Add(operatorStack.Pop());
            if (operatorStack.Count == 0)
            {
                ParserError($"No matching: {Tokens.OPEN}");
            }

            top = operatorStack.Peek();
        }

        _ = operatorStack.Pop();
        if (operatorStack.Count != 0 && operatorStack.Peek().Type == TokenType.Function)
        {
            output.Add(operatorStack.Pop());
        }
    }

    private static void CheckUnary(IList<Token> output, Token? prev, uint line)
    {
        switch ((prev ?? Tokens.DUMMY).Type)
        {
            case TokenType.Identifier:
            case TokenType.Number:
            case TokenType.Parameter:
            case TokenType.Variable:
            case TokenType.Input:
            case TokenType.Output:
            case TokenType.Constant:
            case TokenType.Close:
                return;
        }

        output.Add(Tokens.GetReserved(Tokens.ZERO, line));
    }

    private void OnPrecedence(IList<Token> output, Token token)
    {
        int precedence = token.Precedence();
        bool left = token.LeftAssociative();
        while (operatorStack.Count != 0)
        {
            Token op = operatorStack.Peek();
            TokenType opType = op.Type;
            if (opType != TokenType.Comparison && opType != TokenType.Arithmetic)
            {
                break;
            }

            int pOperator = op.Precedence();
            if (precedence < pOperator || left && precedence == pOperator)
            {
                output.Add(operatorStack.Pop());
            }
            else break;
        }
        operatorStack.Push(token);
    }

    private void OnEmptyQueue(IList<Token> output)
    {
        while (operatorStack.Count != 0)
        {
            Token token = operatorStack.Pop();
            if (token.Type == TokenType.Open)
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
        return false; // unreachable
    }

    private bool Accept(TokenType type)
    {
        bool success = type == currentToken.Type;
        if (success)
        {
            AdvanceToken();
        }
        return success;
    }

    #endregion

    #region Helpers

    private void AdvanceToken()
    {
        if (currentIndex == maxIndex)
        {
            ParserError("End reached unexpectedly!");
        }

        previousToken = currentToken;
        currentToken = lexicalTokens[++currentIndex];
    }

    #endregion

    private void ParserError(string error)
    {
        throw new ParserException(error, currentToken.Line);
    }
}
