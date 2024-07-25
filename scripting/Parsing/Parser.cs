using scripting.Lexing;
using scripting.Script;
using System.Collections.Generic;
using System.Diagnostics;

namespace scripting.Parsing;

/// <summary>
/// Can parse a list of Tokens.
/// </summary>
public class Parser : IParser
{
    #region Fields

    private readonly HashSet<string> parameterNames = new(Constants.MAX_PARAMETERS);
    private readonly HashSet<string> variableNames = new(Constants.MAX_VARIABLES);

    private readonly Queue<Token> tokenBuffer = new();
    private readonly Stack<Operator> operatorStack = new();

    private int currentIndex;
    private readonly int maxIndex;

    private Token previousToken = Tokens.DUMMY;
    private Token currentToken;
    private readonly ITokenList lexicalTokens;
    
    // keeps track of amount of recursion, could also consider using explicit stack
    private int depth;

    private readonly string description;
    private readonly Parameters parameters = [];
    private readonly List<ASTAssign> variables = [];
    private readonly Dictionary<string, ParsedCallback> callbacks = [];

    #endregion

    #region Constructors

    /// <summary>
    /// Parses the input list of Tokens.
    /// </summary>
    /// <param name="tokens">List of tokens from the script</param>
    public Parser(LexingResult tokens)
    {
        description = tokens.Description;
        lexicalTokens = tokens.Tokens;

        maxIndex = lexicalTokens.Count - 1;
        if (maxIndex < 3)
        {
            throw ParserError($"Script is too small (maximum index = {maxIndex}) to have required sections!");
        }

        currentToken = lexicalTokens[currentIndex];
        Debug.Assert(currentToken.Type == TokenType.SquareOpen);
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
        AdvanceToken();
        while (currentToken.Type != TokenType.SquareClose)
        {
            DeclareParameter();
        }

        // Variables
        AdvanceToken();
        while (currentToken.Type != TokenType.CurlyOpen)
        {
            DeclareVariable();
        }

        // Calculation
        List<ASTNode> asts = [];
        AdvanceToken();
        while (currentToken.Type != TokenType.CurlyClose)
        {
            asts.Add(Statement());
        }
        callbacks.Add(Calculation.NAME, new(Calculation.NAME, [], asts));

        if (currentIndex == maxIndex) return;

        // Optional Callbacks
        AdvanceToken();
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
                throw ParserError("Undefined state reached after attempting to parse bounds!");
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
                case TokenType.Number:
                case TokenType.Bool:
                case TokenType.Constant:
                    output.Add(token);
                    break;
                case TokenType.Identifier:
                    output.Add(ResolveIdentifier(token));
                    break;
                case TokenType.Arithmetic:
                    bool unary = CheckUnary(output, token, prev);
                    OnPrecedence(output, token, unary);
                    break;
                case TokenType.ArgumentSeparator:
                    OnSeparator(output);
                    break;
                case TokenType.Function:
                case TokenType.ParenOpen:
                    OnOpen(token);
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

        variables.Add(new(t, eq, output));
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

    private ASTNode Statement()
    {
        ASTTag tag;
        ASTUnion union;

        bool isAssignment =
            Accept(TokenType.Identifier) ||
            Accept(TokenType.Input) ||
            Accept(TokenType.Output);
        if (isAssignment)
        {
            Token target = previousToken;
            if (target.Type == TokenType.Identifier)
            {
                if (!variableNames.Contains(target.Symbol))
                {
                    throw ParserError($"Only variables can be assigned to! {target.Symbol} is not a known variable.");
                }

                target = target.WithType(TokenType.Variable);
            }

            Expect(TokenType.Assignment);
            Token assignment = previousToken;

            TokenList initializer = Expression(TokenType.Terminator);

            tag = ASTTag.Assign;
            union = new ASTUnion()
            {
                astAssign = new ASTAssign(target, assignment, initializer)
            };
        }
        else if (Accept(TokenType.Return))
        {
            Expect(TokenType.Terminator);
            tag = ASTTag.Return;
            union = new ASTUnion()
            {
                astReturn = new ASTReturn()
            };
        }
        else if (Accept(TokenType.If))
        {
            Expect(TokenType.ParenOpen);
            TokenList condition = Expression(TokenType.ParenClose, TokenType.CurlyOpen);

            IBlock ifBlock = CollectStatements();

            IBlock? elseBlock = null;
            if (Accept(TokenType.Else))
            {
                elseBlock = CollectStatements();
            }

            tag = ASTTag.If;
            union = new ASTUnion()
            {
                astIf = new ASTIf(condition, ifBlock, elseBlock)
            };
        }
        else if (Accept(TokenType.While))
        {
            Expect(TokenType.ParenOpen);
            TokenList condition = Expression(TokenType.ParenClose, TokenType.CurlyOpen);

            IBlock whileBlock = CollectStatements();

            tag = ASTTag.While;
            union = new ASTUnion()
            {
                astWhile = new ASTWhile(condition, whileBlock)
            };
        }
        else
        {
            throw ParserError($"Expected statement, got: {currentToken.Base}");
        }

        return new ASTNode(tag, union);
    }

    private Block CollectStatements()
    {
        if (++depth > Constants.MAX_RECURSION_DEPTH)
        {
            throw ParserError("Exceeded Maximum Recursion Depth!");
        }

        // collection expression on Block should work because it's well-behaved
        Block asts = [];
        Expect(TokenType.CurlyOpen);
        while (!Accept(TokenType.CurlyClose)) asts.Add(Statement());

        --depth;
        return asts;
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
            bool nextIsExpected = noAfter || after == nextType;
            if (currentType == end && nextIsExpected)
            {
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
                case TokenType.Bool:
                case TokenType.Constant:
                case TokenType.Input:
                case TokenType.Output:
                    output.Add(token);
                    break;
                case TokenType.Identifier:
                    output.Add(ResolveIdentifier(token));
                    break;
                case TokenType.Arithmetic:
                    bool unary = CheckUnary(output, token, prev);
                    OnPrecedence(output, token, unary);
                    break;
                case TokenType.Comparison:
                    OnPrecedence(output, token);
                    break;
                case TokenType.ArgumentSeparator:
                    OnSeparator(output);
                    break;
                case TokenType.Function:
                case TokenType.ParenOpen:
                    OnOpen(token);
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

    #endregion

    #region Expression Helpers

    private void OnOpen(Token token)
    {
        operatorStack.Push(new(token, -1));
    }

    private void OnClose(TokenList output)
    {
        Operator? oper;
        while (operatorStack.TryPop(out oper) && oper.Type != TokenType.ParenOpen)
        {
            output.Add(oper.Token);
        }
        // the parenthesis is discarded intentionally (if the operator is not null)

        // if popping fails, there was no matching opening parenthesis before the bottom of the stack
        if (oper is null)
        {
            throw ParserError($"No matching: {Tokens.PAREN_OPEN}");
        }

        if (operatorStack.TryPeek(out var maybeFunction) && maybeFunction.Type == TokenType.Function)
        {
            output.Add(operatorStack.Pop().Token);
        }
    }

    private static bool CheckUnary(TokenList output, Token token, Token? prev)
    {
        switch ((prev ?? Tokens.DUMMY).Type)
        {
            case TokenType.Number:
            case TokenType.Bool:
            case TokenType.Constant:
            case TokenType.Identifier:
            case TokenType.Parameter:
            case TokenType.Variable:
            case TokenType.Input:
            case TokenType.Output:
            case TokenType.ParenClose:
                return false;
        }

        output.Add(Tokens.GetReserved(Tokens.ZERO, token.Line));
        return true;
    }

    private void OnPrecedence(TokenList output, Token token, bool unary = false)
    {
        Operator tokenOperator = new(token, token.Precedence(unary));
        bool left = token.LeftAssociative();
        while (operatorStack.TryPop(out var oper))
        {
            if (oper.HasHigherPrecedence(tokenOperator, left))
            {
                output.Add(oper.Token);
            }
            else
            {
                operatorStack.Push(oper);
                break;
            }
        }
        operatorStack.Push(tokenOperator);
    }

    private void OnSeparator(TokenList output)
    {
        while (operatorStack.TryPop(out var oper))
        {
            if (oper.Type == TokenType.ParenOpen)
            {
                // the opening parenthesis should be discarded by a closing one
                
                // analysis -> loop(TryPop) then Push vs loop(TryPeek then Pop):
                // where n is the amount of elements up to and including the goal,
                // or the total length of the stack if the goal is not present
                // n+1 vs 2n-1 if the parenthesis is there
                // n vs 2n if the parenthesis is not there
                // so we use the former, same logic applies in OnPrecedence
                operatorStack.Push(oper);
                return;
            }

            output.Add(oper.Token);
        }

        // if popping fails, this was just a random separator
        throw ParserError($"Unexpected: {Tokens.ARG_SEP}");
    }

    private void OnEmptyQueue(TokenList output)
    {
        while (operatorStack.TryPop(out var oper))
        {
            Token token = oper.Token;
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

        IBlock code = CollectStatements();

        return new(name, args, code);
    }

    #endregion

    #region Helpers

    private void Expect(TokenType type)
    {
        if (Accept(type))
        {
            return;
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

    private Token ResolveIdentifier(Token token)
    {
        Token resolved;
        if (parameterNames.Contains(token.Symbol))
        {
            resolved = token.WithType(TokenType.Parameter);
        }
        else if (variableNames.Contains(token.Symbol))
        {
            resolved = token.WithType(TokenType.Variable);
        }
        else
        {
            throw ParserError("Could not resolve variable name!");
        }
        return resolved;
    }

    #endregion

    private ParserException ParserError(string error)
    {
        return new ParserException(error, currentToken.Line);
    }
}
