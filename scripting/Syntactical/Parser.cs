﻿using scripting.Common;
using scripting.Lexical;
using scripting.Script;
using System.Collections.Generic;
using System.Diagnostics;

namespace scripting.Syntactical;

/// <summary>
/// Can parse a list of Tokens.
/// </summary>
public class Parser : IParser
{
    #region Fields

    private readonly HashSet<string> parameterNames = new(Constants.MAX_PARAMETERS);
    private readonly HashSet<string> variableNames = new(Constants.MAX_VARIABLES);

    private readonly Queue<Token> tokenBuffer = new();
    private readonly Stack<Token> operatorStack = new();

    private int currentIndex;
    private readonly int maxIndex;

    private Token previousToken;
    private Token currentToken;
    private readonly ITokenList lexicalTokens;

    private readonly string description;
    private readonly Parameters parameters = [];
    private readonly List<Variable> variables = [];
    private readonly Dictionary<string, ParsedCallback> callbacks = [];

    #endregion

    #region Constructors

    /// <summary>
    /// Parses the input list of Tokens.
    /// </summary>
    /// <param name="tokens">List of tokens from the script.</param>
    public Parser(LexingResult tokens)
    {
        description = tokens.Description;
        lexicalTokens = tokens.Tokens;
        Debug.Assert(lexicalTokens.Count >= 1, "Tokenizer did not prevent empty script!");

        currentToken = lexicalTokens[0];
        Debug.Assert(currentToken.Type == TokenType.SquareOpen);

        maxIndex = lexicalTokens.Count - 1;
        if (maxIndex <= 1)
        {
            throw ParserError("Nearly empty script!");
        }

        AdvanceToken();
        Debug.Assert(currentIndex == 1, "We don't need to check for PARAMS_START at runtime.");

        previousToken = currentToken;
    }

    #endregion

    #region Methods

    public ParsingResult Parse()
    {
        ParseTokens();
        return new(description, parameters, variables, [.. callbacks.Values]);
    }

    private void ParseTokens()
    {
        // Parameters
        while (currentToken.Type != TokenType.SquareClose)
        {
            DeclareParameter();
        }
        CoerceAll(parameterNames, TokenType.Parameter);

        AdvanceToken();
        Debug.Assert(currentToken.Type != TokenType.SquareClose);

        // Variables
        while (currentToken.Type != TokenType.CurlyOpen)
        {
            DeclareVariable();
        }
        CoerceAll(variableNames, TokenType.Variable);

        AdvanceToken();
        Debug.Assert(currentToken.Type != TokenType.CurlyOpen);

        // Calculation
        TokenList syntacticTokens = [];
        while (currentToken.Type != TokenType.CurlyClose)
        {
            syntacticTokens.AddRange(Statement());
        }
        callbacks.Add(Calculation.NAME, new(Calculation.NAME, [], syntacticTokens));

        if (currentIndex == maxIndex)
            return;

        AdvanceToken();
        Debug.Assert(currentToken.Type != TokenType.CurlyClose);

        // Optional Callbacks
        while (Accept(TokenType.Identifier))
        {
            ParsedCallback callback = ParseOptionalCallback();
            if (callbacks.ContainsKey(callback.Name))
            {
                throw ParserError("Duplicate callbacks detected!");
            }

            callbacks[callback.Name] = callback;
        }
    }

    private void CoerceAll(HashSet<string> identifiers, TokenType type)
    {
        for (int i = 0; i <= maxIndex; i++)
        {
            Token token = lexicalTokens[i];
            if (identifiers.Contains(token.Symbol))
            {
                lexicalTokens[i] = token.WithType(type);
            }
        }
    }

    #endregion

    #region Parameter Declarations

    private void DeclareParameter()
    {
        Token token = CoerceIdentifier(TokenType.Parameter);
        string symbol = token.Symbol;

        Debug.Assert(parameterNames.Count <= Constants.MAX_PARAMETERS);
        if (parameterNames.Count == Constants.MAX_PARAMETERS)
        {
            throw ParserError($"Too many parameters! (max {Constants.MAX_PARAMETERS})");
        }
        else if (parameterNames.Contains(symbol))
        {
            throw ParserError("Parameter name already exists!");
        }

        parameterNames.Add(symbol);

        tokenBuffer.Enqueue(token);
        BufferExpectedToken(TokenType.Assignment);
        if (DeclAccept(TokenType.Bool))
        {
            TerminateParameter(out var nameB, out var valueB);
            parameters.Add(new(nameB, valueB, new(), new()));
            return;
        }

        BufferExpectedToken(TokenType.Number);
        ParseBounds(out var minval, out var maxval);
        TerminateParameter(out var name, out var value);
        
        parameters.Add(new(name, value, minval, maxval));
    }

    private void TerminateParameter(out Token name, out Token value)
    {
        _ = DeclExpect(TokenType.Terminator);

        name = tokenBuffer.Dequeue();
        Token eq = tokenBuffer.Dequeue();
        value = tokenBuffer.Dequeue();

        if (eq.Symbol != Tokens.ASSIGN)
        {
            throw ParserError($"Expected {Tokens.ASSIGN}");
        }
    }

    private void ParseBounds(out ParameterValidation minval, out ParameterValidation maxval)
    {
        bool hasBounds =
            Accept(TokenType.SquareOpen) ||
            Accept(TokenType.ParenOpen) ||
            Accept(TokenType.CurlyOpen);
        if (!hasBounds)
        {
            minval = new();
            maxval = new();
            return;
        }

        TokenType open = previousToken.Type;
        switch (open)
        {
            case TokenType.SquareOpen:
                Expect(TokenType.Number);
                minval = new(Bound.LowerIncl, (Number)previousToken);
                break;
            case TokenType.ParenOpen:
                Expect(TokenType.Number);
                minval = new(Bound.LowerExcl, (Number)previousToken);
                break;
            case TokenType.CurlyOpen:
                minval = new();
                break;
            default:
                // this is unreachable as long as one of the cases is accepted at the top of the method
                Debug.Fail($"Unreachable: unknown bound open {open}");
                minval = new();
                maxval = new();
                return;
        }

        // the edge case {} is technically not considered here
        // if someone wants to explicitly denote 'no bounds' we let them
        if (Accept(TokenType.CurlyClose))
        {
            maxval = new();
            return;
        }

        bool noLowerBound = minval.Type == Bound.None;
        if (Accept(TokenType.ArgumentSeparator))
        {
            if (noLowerBound)
            {
                throw ParserError($"Number between '{Tokens.CURLY_OPEN}' and '{Tokens.ARG_SEP}'!");
            }

            Expect(TokenType.Number);
        }
        else if (!noLowerBound)
        {
            throw ParserError("Expected a separator and number for upper bound!");
        }
        else Expect(TokenType.Number);
        
        Number upper = (Number)previousToken;
        if (Accept(TokenType.SquareClose))
        {
            maxval = new(Bound.UpperIncl, upper);
        }
        else if (Accept(TokenType.ParenClose))
        {
            maxval = new(Bound.UpperExcl, upper);
        }
        else if (Accept(TokenType.CurlyClose))
        {
            throw ParserError($"Unexpected number attached to infinite upper bound!");
        }
        else
        {
            throw ParserError($"Unknown upper bound symbol: '{currentToken.Symbol}'!");
        }
    }

    #endregion

    #region Variable Declarations

    private void DeclareVariable()
    {
        Token token = CoerceIdentifier(TokenType.Variable);
        string symbol = token.Symbol;

        Debug.Assert(variableNames.Count <= Constants.MAX_VARIABLES);
        if (variableNames.Count == Constants.MAX_VARIABLES)
        {
            throw ParserError($"Too many variables! (max {Constants.MAX_VARIABLES})");
        }
        else if (parameterNames.Contains(symbol))
        {
            throw ParserError("Parameter/Variable name conflict!");
        }
        else if (variableNames.Contains(symbol))
        {
            throw ParserError("Variable name already exists!");
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
            if (currentToken.Type == TokenType.CurlyOpen)
            {
                throw ParserError("Calculation block reached unexpectedly!");
            }

            input.Enqueue(currentToken);
            AdvanceToken();
        }
        AdvanceToken();

        Debug.Assert(operatorStack.Count == 0);

        // Shunting Yard Algorithm (RPN)
        TokenList output = new(input.Count);

        Token? prev = null;
        foreach (Token token in input)
        {
            switch (token.Type)
            {
                case TokenType.Identifier:
                    if (!variableNames.Contains(token.Symbol))
                    {
                        throw ParserError("Could not resolve variable name!");
                    }

                    output.Add(token.WithType(TokenType.Variable));
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
                case TokenType.ParenOpen:
                    operatorStack.Push(token);
                    break;
                case TokenType.ParenClose:
                    OnClose(output);
                    break;
                default:
                    throw ParserError("Unexpected expression token!");
            }
            prev = token;
        }
        OnEmptyQueue(output);

        Debug.Assert(tokenBuffer.Count == 2);

        Token t = tokenBuffer.Dequeue();
        Token eq = tokenBuffer.Dequeue();

        if (eq.Symbol != Tokens.ASSIGN)
        {
            throw ParserError($"Expected {Tokens.ASSIGN}");
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

    private Token CoerceIdentifier(TokenType type)
    {
        int previousIndex = DeclExpect(TokenType.Identifier);
        Token token = previousToken.WithType(type);
        lexicalTokens[previousIndex] = token;
        return token;
    }

    private int DeclExpect(TokenType type)
    {
        if (DeclAccept(type))
        {
            return currentIndex - 1;
        }

        throw ParserError("Unexpected declaration!");
    }

    private bool DeclAccept(TokenType type)
    {
        bool success = currentToken.Type == type;
        if (success) AdvanceToken();
        return success;
    }

    #endregion

    #region Calculation

    private TokenList Statement()
    {
        TokenList tokens = [];

        bool isAssignment =
            Accept(TokenType.Variable) ||
            Accept(TokenType.Input) ||
            Accept(TokenType.Output);
        if (isAssignment)
        {
            Token target = previousToken;
            Expect(TokenType.Assignment);

            Token assignment = previousToken;
            tokens.AddRange(Expression(TokenType.Terminator));
            tokens.Add(assignment);
            tokens.Add(target);
        }
        else if (Accept(TokenType.Return))
        {
            Expect(TokenType.Terminator);
        }
        else if (Expect(TokenType.Branch))
        {
            Token branch = previousToken;

            Expect(TokenType.ParenOpen);
            tokens.AddRange(Expression(TokenType.ParenClose, TokenType.CurlyOpen));

            AddStatements(tokens, branch);

            Token maybeElse = currentToken;
            if (branch.Symbol == Tokens.BRANCH_IF && maybeElse.Symbol == Tokens.BRANCH_ELSE)
            {
                Expect(TokenType.Branch);
                Expect(TokenType.CurlyOpen);
                AddStatements(tokens, maybeElse);
            }
        }

        return tokens;
    }

    private void AddStatements(TokenList tokens, Token token)
    {
        tokens.Add(token);

        do tokens.AddRange(Statement());
        while (!Accept(TokenType.CurlyClose));

        tokens.Add(Tokens.GetReserved(Tokens.BRANCH_END, previousToken.Line));
    }

    private TokenList Expression(TokenType end, TokenType after = TokenType.Undefined)
    {
        bool noAfter = after == TokenType.Undefined;

        Queue<Token> input = new();
        while (true)
        {
            Token current = currentToken;
            TokenType currentType = current.Type;
            if (currentType == TokenType.CurlyClose)
            {
                throw ParserError("Block end reached unexpectedly!");
            }

            AdvanceToken();
            TokenType nextType = currentToken.Type;
            bool afterIsNext = after == nextType;
            if (currentType == end && (noAfter || afterIsNext))
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
        TokenList output = new(input.Count);

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
                case TokenType.ParenOpen:
                    operatorStack.Push(token);
                    break;
                case TokenType.ParenClose:
                    OnClose(output);
                    break;
                default:
                    throw ParserError("Unexpected expression token!");
            }
            prev = token;
        }
        OnEmptyQueue(output);
        return output;
    }

    private void OnClose(TokenList output)
    {
        Token top;
        if (!operatorStack.TryPeek(out top!))
        {
            throw ParserError($"Unexpected: {Tokens.PAREN_CLOSE}");
        }

        while (top.Type != TokenType.ParenOpen)
        {
            output.Add(operatorStack.Pop());
            if (operatorStack.Count == 0)
            {
                throw ParserError($"No matching: {Tokens.PAREN_OPEN}");
            }

            top = operatorStack.Peek();
        }

        _ = operatorStack.Pop();
        if (operatorStack.Count != 0 && operatorStack.Peek().Type == TokenType.Function)
        {
            output.Add(operatorStack.Pop());
        }
    }

    private static void CheckUnary(TokenList output, Token? prev, uint line)
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
            case TokenType.ParenClose:
                return;
        }

        output.Add(Tokens.GetReserved(Tokens.ZERO, line));
    }

    private void OnPrecedence(TokenList output, Token token)
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

    private void OnEmptyQueue(TokenList output)
    {
        while (operatorStack.Count != 0)
        {
            Token token = operatorStack.Pop();
            if (token.Type == TokenType.ParenOpen)
            {
                throw ParserError($"No matching: {Tokens.PAREN_CLOSE}");
            }

            output.Add(token);
        }

        if (output.Count == 0)
        {
            throw ParserError("Empty expression!");
        }
    }

    private bool Expect(TokenType type)
    {
        if (Accept(type))
        {
            return true;
        }

        throw ParserError("Unexpected token!");
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

    #region Optional Callbacks

    private ParsedCallback ParseOptionalCallback()
    {
        string name = previousToken.Symbol;

        TokenList args = [];
        if (Accept(TokenType.ParenOpen))
        {
            do args.Add(currentToken);
            while (Accept(TokenType.ArgumentSeparator));
            Expect(TokenType.ParenClose);
        }

        TokenList code = [];
        Expect(TokenType.CurlyOpen);
        while (currentToken.Type != TokenType.CurlyClose)
        {
            code.AddRange(Statement());
        }
        Expect(TokenType.CurlyClose);

        return new(name, args, code);
    }

    #endregion

    #region Helpers

    private void AdvanceToken()
    {
        Debug.Assert(currentIndex <= maxIndex);
        if (currentIndex == maxIndex)
        {
            throw ParserError("End reached unexpectedly!");
        }

        previousToken = currentToken;
        currentToken = lexicalTokens[++currentIndex];
    }

    #endregion

    private ParserException ParserError(string error)
    {
        return new ParserException(error, currentToken.Line);
    }
}
