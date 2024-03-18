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

    #endregion Fields

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
        Debug.Assert(CmpCurrentTokenType(TokenType.ParameterStart));

        maxIndex = lexicalTokens.Count - 1;
        Debug.Assert(maxIndex > 0);

        AdvanceToken();
        Debug.Assert(currentIndex == 1, "We don't need to check for PARAMS_START at runtime.");

        previousToken = currentToken;
    }

    #endregion Constructors

    #region Methods

    public ParsingResult Parse()
    {
        ParseTokens();
        return new(description, parameters, variables, syntacticTokens);
    }

    private void ParseTokens()
    {
        while (!CmpCurrentTokenType(TokenType.ParameterEnd))
        {
            DeclParam();
        }

        AdvanceToken();
        Debug.Assert(!CmpCurrentTokenType(TokenType.ParameterEnd));

        CoerceAll(parameterNames, TokenType.Parameter);

        // Parameters MUST be coerced before this!
        while (!CmpCurrentTokenType(TokenType.CalculationStart))
        {
            DeclVar();
        }

        AdvanceToken();
        Debug.Assert(!CmpCurrentTokenType(TokenType.CalculationStart));

        CoerceAll(variableNames, TokenType.Variable);

        // Calculation
        while (!CmpCurrentTokenType(TokenType.CalculationEnd))
        {
            Statement();
        }
    }

    #endregion Methods

    #region Helpers

    private void CoerceAll(ISet<string> identifiers, TokenType type)
    {
        for (int i = 0; i <= maxIndex; i++)
        {
            Token token = lexicalTokens[i];
            if (identifiers.Contains(token.Base.Symbol))
            {
                lexicalTokens[i] = token with { Base = token.Base with { Type = type } };
            }
        }
    }

    private bool CmpCurrentTokenType(TokenType type)
    {
        return currentToken.Base.Type == type;
    }

    private void AdvanceToken()
    {
        if (currentIndex == maxIndex)
        {
            ParserError("End reached unexpectedly!");
        }

        previousToken = currentToken;
        currentToken = lexicalTokens[++currentIndex];
    }

    #endregion Helpers

    #region Declarations

    private void DeclParam()
    {
        Token? DequeueMaybeDummy() => tokenBuffer.Dequeue().NullifyUndefined();

        int previousIndex = DeclExpect(TokenType.Identifier);
        Token token = previousToken;
        token = token with { Base = token.Base with { Type = TokenType.Parameter } };
        lexicalTokens[previousIndex] = token;

        Debug.Assert(parameterNames.Count <= Constants.MAX_PARAMETERS);
        if (parameterNames.Count == Constants.MAX_PARAMETERS)
        {
            ParserError($"Too many parameters! (max {Constants.MAX_PARAMETERS})");
        }

        parameterNames.Add(token.Base.Symbol);
        tokenBuffer.Enqueue(token);

        BufferExpectedToken(TokenType.Assignment);
        if (DeclAccept(TokenType.Boolean))
        {
            TerminateParam(out Token nameB, out Token valueB);
            parameters.Add(new(nameB, valueB, null, null, null, null));
            return;
        }

        BufferExpectedToken(TokenType.Number);
        ParseGuards();
        TerminateParam(out Token name, out Token value);
        
        Token? minGuard = DequeueMaybeDummy();
        Token? min      = DequeueMaybeDummy();
        Token? maxGuard = DequeueMaybeDummy();
        Token? max      = DequeueMaybeDummy();

        parameters.Add(new(name, value, minGuard, min, maxGuard, max));
    }

    private void TerminateParam(out Token name, out Token value)
    {
        _ = DeclExpect(TokenType.Terminator);

        name = tokenBuffer.Dequeue();
        Token eq = tokenBuffer.Dequeue();
        value = tokenBuffer.Dequeue();

        if (eq.Base.Symbol != Tokens.ASSIGN)
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

    private void DeclVar()
    {
        int previousIndex = DeclExpect(TokenType.Identifier);
        Token token = previousToken;
        token = token with { Base = token.Base with { Type = TokenType.Variable } };
        lexicalTokens[previousIndex] = token;

        Debug.Assert(variableNames.Count <= Constants.MAX_VARIABLES);
        if (variableNames.Count == Constants.MAX_VARIABLES)
        {
            ParserError($"Too many variables! (max {Constants.MAX_VARIABLES})");
        }
        else if (parameterNames.Contains(token.Base.Symbol))
        {
            ParserError("Parameter/Variable name conflict!");
        }

        variableNames.Add(token.Base.Symbol);
        tokenBuffer.Enqueue(token);

        BufferExpectedToken(TokenType.Assignment);

        ExprVar();
        AdvanceToken();
    }

    private void ExprVar()
    {
        Queue<Token> input = new();
        while (currentToken.Base.Type != TokenType.Terminator)
        {
            if (currentToken.Base.Type == TokenType.CalculationStart)
            {
                ParserError("Calculation block reached unexpectedly!");
            }

            input.Enqueue(currentToken);
            AdvanceToken();
        }

        Debug.Assert(operatorStack.Count == 0);

        // Shunting Yard Algorithm (RPN)
        IList<Token> output = new List<Token>(input.Count);
        foreach (Token token in input)
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
                    operatorStack.Push(token);
                    continue;
                case TokenType.Close:
                    OnClose(output);
                    continue;
                default:
                    ParserError("Unexpected expression token!");
                    break;
            }
        OnEmptyQueue(output);

        Debug.Assert(tokenBuffer.Count == 2);

        Token t = tokenBuffer.Dequeue();
        Token eq = tokenBuffer.Dequeue();

        if (eq.Base.Symbol != Tokens.ASSIGN)
        {
            ParserError($"Expected {Tokens.ASSIGN}");
        }

        output.Add(eq);
        output.Add(t);
        variables.Add(new(t.Base.Symbol, output));
    }

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
        bool success = CmpCurrentTokenType(type);
        if (success)
        {
            AdvanceToken();
        }
        return success;
    }

    #endregion Declarations

    #region Calculation

    private void Statement()
    {
        if (Accept(TokenType.Variable) ||
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
        else if (Expect(TokenType.Branch))
        {
            Token branch = previousToken;
            Expect(TokenType.Open);
            syntacticTokens.AddRange(Expression(TokenType.Close, TokenType.Block));
            syntacticTokens.Add(branch);

            do Statement();
            while (!Accept(TokenType.Block));
            syntacticTokens.Add(Tokens.ReservedMap[Tokens.BRANCH_END] with { Line = previousToken.Line });
        }
    }

    private IList<Token> Expression(TokenType end, TokenType after = TokenType.Undefined)
    {
        Queue<Token> input = new();
        while (true)
        {
            Token current = currentToken;
            TokenType currentType = current.Base.Type;

            if (currentType == TokenType.CalculationEnd)
            {
                ParserError("Calculation block end reached unexpectedly!");
            }

            AdvanceToken();
            TokenType nextType = currentToken.Base.Type;
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

    private IList<Token> ShuntingYard(Queue<Token> input)
    {
        Debug.Assert(operatorStack.Count == 0);

        // Shunting Yard Algorithm (RPN)
        IList<Token> output = new List<Token>(input.Count);
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
                    operatorStack.Push(token);
                    continue;
                case TokenType.Close:
                    OnClose(output);
                    continue;
                default:
                    ParserError("Unexpected expression token!");
                    break; // unreachable
            }
        }
        OnEmptyQueue(output);
        return output;
    }

    private void OnClose(IList<Token> output)
    {
        Token top;
        try
        {
            top = operatorStack.Peek();
        }
        catch (InvalidOperationException)
        {
            ParserError($"Unexpected: {Tokens.CLOSE}");
            return;
        }

        while (top.Base.Type != TokenType.Open)
        {
            output.Add(operatorStack.Pop());

            if (operatorStack.Count == 0)
            {
                ParserError($"No matching: {Tokens.OPEN}");
            }

            top = operatorStack.Peek();
        }

        Debug.Assert(top.Base.Type == TokenType.Open);

        _ = operatorStack.Pop();
        if (operatorStack.Count != 0 && operatorStack.Peek().Base.Type == TokenType.Function)
        {
            output.Add(operatorStack.Pop());
        }
    }

    private void OnPrecedence(IList<Token> output, Token token)
    {
        int pToken = token.Precedence();
        bool left = token.LeftAssociative();
        while (operatorStack.Count != 0)
        {
            Token op = operatorStack.Peek();
            TokenType optype = op.Base.Type;
            if (optype != TokenType.Comparison && optype != TokenType.Arithmetic)
            {
                break;
            }

            int pOperator = op.Precedence();
            if (pToken < pOperator || left && pToken == pOperator)
            {
                output.Add(operatorStack.Pop());
            }
            else
            {
                break;
            }
        }
        operatorStack.Push(token);
    }

    private void OnEmptyQueue(IList<Token> output)
    {
        while (operatorStack.Count != 0)
        {
            Token token = operatorStack.Pop();
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
        return false; // unreachable
    }

    private bool Accept(TokenType type)
    {
        bool success = type == currentToken.Base.Type;
        if (success)
        {
            AdvanceToken();
        }
        return success;
    }

    #endregion Calculation

    private void ParserError(string error)
    {
        throw new ParserException(error, currentToken.Line);
    }
}
