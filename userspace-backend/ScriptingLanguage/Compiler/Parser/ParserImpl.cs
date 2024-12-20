using System.Collections.Generic;
using System.Diagnostics;
using userspace_backend.ScriptingLanguage.Compiler.Tokenizer;
using userspace_backend.ScriptingLanguage.Script;

namespace userspace_backend.ScriptingLanguage.Compiler.Parser;

/// <summary>
/// Can parse a list of Tokens.
/// </summary>
public class ParserImpl : IParser
{
    #region Fields

    private readonly CompilerContext context;
    private readonly IList<Token> lexicalTokens;

    private int currentIndex;
    private readonly int maxIndex;

    private Token previousToken = default;
    private Token currentToken;

    private readonly Dictionary<string, TokenType> declarationNames = new(Constants.CAPACITY);
    private readonly HashSet<string> functionLocalNames = [];

    // keeps track of amount of recursion, could also consider using explicit stack
    private int depth;
    private readonly Stack<Operator> operatorStack = new();

    private readonly string description;
    private readonly Parameters parameters = [];
    private readonly Block declarations = [];
    private readonly Dictionary<string, ParsedCallback> callbacks = [];

    #endregion

    #region Constructors

    /// <summary>
    /// Parses the input list of Tokens.
    /// </summary>
    /// <param name="tokens">List of tokens from the script.</param>
    public ParserImpl(LexingResult tokens)
    {
        context = tokens.Context;
        description = tokens.Description;
        lexicalTokens = tokens.Tokens;

        maxIndex = lexicalTokens.Count - 1;
        if (maxIndex < 3)
            throw ParserError($"Script is too small (maximum index = {maxIndex}) to have required sections!");

        currentToken = lexicalTokens[currentIndex];
        Debug.Assert(currentToken.Type == TokenType.SquareOpen);
    }

    #endregion

    #region Methods

    public ParsingResult Parse()
    {
        ParseTokens();

        if (parameters.Count > Constants.MAX_PARAMETERS)
            throw ParserError($"Too many parameters! Expected at most {Constants.MAX_PARAMETERS}, got {parameters.Count}.");

        // this can be expanded a bit more since persistent and impersistent are separated, but for now it'll work
        if (declarations.Count > Constants.MAX_DECLARATIONS)
            throw ParserError($"Too many declarations! Expected at most {Constants.MAX_DECLARATIONS}, got {declarations.Count}.");

        return new(context, description, parameters, declarations, [.. callbacks.Values]);
    }

    private void ParseTokens()
    {
        // Parameters
        AdvanceToken();
        while (currentToken.Type != TokenType.SquareClose)
            ParseParameter();

        // Declarations
        AdvanceToken();
        while (currentToken.Type != TokenType.CurlyOpen)
            ParseDeclaration();

        // Calculation
        AdvanceToken();
        Block asts = [];
        while (currentToken.Type != TokenType.CurlyClose)
            asts.Add(Statement());
        callbacks.Add(Calculation.NAME, new(Calculation.NAME, [], [.. asts]));

        if (currentIndex == maxIndex)
            return;

        // Optional Callbacks
        AdvanceToken();
        while (Accept(TokenType.Identifier, out var identifier))
        {
            List<Token> args = ParseArgs();
            Block code = ParseBlock();

            ParsedCallback callback = new(context.GetSymbol(identifier), [.. args], [.. code]);
            if (callbacks.ContainsKey(callback.Name))
                throw ParserError("Duplicate callbacks detected!");

            callbacks[callback.Name] = callback;
        }
    }

    #endregion

    #region Parameter Parsing

    private void ParseParameter()
    {
        Token identifier = Expect(TokenType.Identifier) with { Type = TokenType.Parameter };
        string symbol = context.GetSymbol(identifier);
        if (!declarationNames.TryAdd(symbol, TokenType.Parameter))
            throw ParserError($"Name collision! Name {symbol} already exists.");

        Discard(TokenType.Assignment);
        // yawn
#pragma warning disable IDE0018 // Inline variable declaration
        Token value;
#pragma warning restore IDE0018 // Inline variable declaration
        ParameterValidation minval, maxval;
        if (Accept(TokenType.Bool, out value))
        {
            minval = default;
            maxval = default;
        }
        else if (Accept(TokenType.Number, out value))
        {
            ParseBounds(out minval, out maxval);
        }
        else
        {
            throw ParserError("Expected either a boolean or numeric value for the parameter!");
        }

        Discard(TokenType.Terminator);
        parameters.Add(new(context, identifier, value, minval, maxval));
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

        Token lower;
        TokenType open = previousToken.Type;
        switch (open)
        {
            case TokenType.SquareOpen:
                lower = Expect(TokenType.Number);
                minval = new(Bound.LowerIncl, Number.Parse(context.GetSymbol(lower), lower));
                break;
            case TokenType.ParenOpen:
                lower = Expect(TokenType.Number);
                minval = new(Bound.LowerExcl, Number.Parse(context.GetSymbol(lower), lower));
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

        Token upper;
        bool noLowerBound = minval.Type == Bound.None;
        if (Accept(TokenType.ArgumentSeparator))
        {
            if (noLowerBound)
                throw ParserError($"Number between '{Tokens.CURLY_OPEN}' and '{Tokens.ARG_SEP}'!");

            upper = Expect(TokenType.Number);
        }
        else if (!noLowerBound)
        {
            throw ParserError("Expected a separator and number for upper bound!");
        }
        else
        {
            upper = Expect(TokenType.Number);
        }

        if (Accept(TokenType.SquareClose))
        {
            maxval = new(Bound.UpperIncl, Number.Parse(context.GetSymbol(upper), upper));
        }
        else if (Accept(TokenType.ParenClose))
        {
            maxval = new(Bound.UpperExcl, Number.Parse(context.GetSymbol(upper), upper));
        }
        else if (Accept(TokenType.CurlyClose))
        {
            throw ParserError($"Unexpected number attached to infinite upper bound!");
        }
        else
        {
            throw ParserError($"Unknown upper bound symbol: '{context.GetSymbol(currentToken)}'!");
        }
    }

    #endregion

    #region Declaration Parsing

    private void ParseDeclaration()
    {
        Token declarer = currentToken;
        TokenType type = declarer.MapDeclarer();
        if (type == TokenType.None)
            throw ParserError("Unknown declarer!");
        else // couldn't use Expect due to mapping, so we have to advance manually
            AdvanceToken();

        Token identifier = Expect(TokenType.Identifier) with { Type = type };
        string symbol = context.GetSymbol(identifier);
        if (declarationNames.ContainsKey(symbol))
            throw ParserError($"Name collision! Name {symbol} already exists.");

        ASTTag tag;
        ASTUnion union;
        if (type == TokenType.Function)
        {
            List<Token> args = ParseArgs();
            foreach (Token arg in args)
                functionLocalNames.Add(context.GetSymbol(arg));

            Block code = ParseBlock();
            functionLocalNames.Clear();

            tag = ASTTag.Function;
            union = new()
            {
                astFunction = new(identifier, [.. code])
            };
        }
        else
        {
            Token eq = Expect(TokenType.Assignment);

            List<Token> output = ExprVar();

            tag = ASTTag.Assign;
            union = new()
            {
                astAssign = new(identifier, eq, [.. output])
            };
        }
        declarations.Add(new ASTNode(tag, union));

        // this is done last to avoid having to check for circular dependencies on this variable
        declarationNames.Add(symbol, type);
    }

    private List<Token> ExprVar()
    {
        Queue<Token> input = new();
        while (!Accept(TokenType.Terminator))
        {
            if (Accept(TokenType.CurlyOpen))
                throw ParserError("Calculation block reached unexpectedly!");

            input.Enqueue(currentToken);
            AdvanceToken();
        }

        Debug.Assert(operatorStack.Count == 0);

        // Shunting Yard Algorithm (RPN)
        List<Token> output = new(input.Count);

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
                    OnIdentifier(output, token);
                    break;
                case TokenType.Arithmetic:
                    bool unary = CheckUnary(output, token, prev);
                    OnPrecedence(output, token, unary);
                    break;
                case TokenType.ArgumentSeparator:
                    OnSeparator(output);
                    break;
                case TokenType.MathFunction:
                    OnFunction(token);
                    break;
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

    #region Block Parsing

    private Block ParseBlock()
    {
        if (++depth > Constants.MAX_RECURSION_DEPTH)
            throw ParserError("Exceeded Maximum Recursion Depth!");

        // collection expression on Block should work because it's well-behaved
        Block asts = [];
        Discard(TokenType.CurlyOpen);
        while (!Accept(TokenType.CurlyClose))
            asts.Add(Statement());

        --depth;
        return asts;
    }

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
                string name = context.GetSymbol(target);
                if (!ResolveIdentifier(name, out TokenType type))
                    throw ParserError($"Unknown assignment target! {name} has not been declared.");

                switch (type)
                {
                    case TokenType.Parameter:
                        throw ParserError("Cannot assign to parameter!");
                    case TokenType.Immutable:
                        throw ParserError("Cannot assign to immutable variable!");
                    case TokenType.Persistent:
                    case TokenType.Impersistent:
                    case TokenType.FunctionLocal:
                        break;
                    default:
                        Debug.Fail("Unreachable: got an unknown TokenType...");
                        break;
                }

                target = target with { Type = type };
            }

            Token assignment = Accept(TokenType.Assignment) ? previousToken : Expect(TokenType.Compound);

            List<Token> initializer = Expression(TokenType.Terminator);

            tag = ASTTag.Assign;
            union = new()
            {
                astAssign = new(target, assignment, [.. initializer])
            };
        }
        else if (Accept(TokenType.If))
        {
            Discard(TokenType.ParenOpen);
            List<Token> condition = Expression(TokenType.ParenClose, TokenType.CurlyOpen);

            Block ifBlock = ParseBlock();

            Block elseBlock;
            if (Accept(TokenType.Else))
                elseBlock = ParseBlock();
            else
                elseBlock = [];

            tag = ASTTag.If;
            union = new()
            {
                astIf = new([.. condition], [.. ifBlock], [.. elseBlock])
            };
        }
        else if (Accept(TokenType.While))
        {
            Discard(TokenType.ParenOpen);
            List<Token> condition = Expression(TokenType.ParenClose, TokenType.CurlyOpen);

            Block whileBlock = ParseBlock();

            tag = ASTTag.While;
            union = new()
            {
                astWhile = new([.. condition], [.. whileBlock])
            };
        }
        else if (Accept(TokenType.Return))
        {
            Discard(TokenType.Terminator);

            tag = ASTTag.Return;
            union = new()
            {
                astReturn = new()
            };
        }
        else
        {
            throw ParserError($"Expected statement!");
        }

        return new ASTNode(tag, union);
    }

    private List<Token> Expression(TokenType end, TokenType after = TokenType.None)
    {
        bool noAfter = after == TokenType.None;

        Queue<Token> input = new();
        while (true)
        {
            Token current = currentToken;
            TokenType currentType = current.Type;
            if (currentType == TokenType.CurlyClose)
                throw ParserError("Block end reached unexpectedly!");

            AdvanceToken();
            TokenType nextType = currentToken.Type;
            bool nextIsExpected = noAfter || after == nextType;
            if (currentType == end && nextIsExpected)
                break;

            input.Enqueue(current);
        }
        Debug.Assert(operatorStack.Count == 0);

        // Shunting Yard Algorithm (RPN)
        List<Token> output = new(input.Count);

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
                    OnIdentifier(output, token);
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
                case TokenType.MathFunction:
                    OnFunction(token);
                    break;
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

    #region Statement Helpers

    private List<Token> ParseArgs()
    {
        List<Token> args = [];
        if (Accept(TokenType.ParenOpen) && !Accept(TokenType.ParenClose))
        {
            do args.Add(currentToken);
            while (Accept(TokenType.ArgumentSeparator));
            Discard(TokenType.ParenClose);
        }
        return args;
    }

    #endregion

    #region Expression Helpers

    private void OnIdentifier(List<Token> output, Token token)
    {
        string name = context.GetSymbol(token);
        if (!ResolveIdentifier(name, out TokenType type))
            throw ParserError($"Could not resolve name! (name was: {name})");

        Token resolved = token with { Type = type };
        if (resolved.Type == TokenType.Function)
            OnFunction(resolved);
        else
            output.Add(resolved);
    }

    private void OnFunction(Token token)
    {
        OnOpen(token); // just does the same (for now...)
    }

    private void OnOpen(Token token)
    {
        operatorStack.Push(new(token, -1));
    }

    private void OnClose(List<Token> output)
    {
        Operator? oper;
        while (operatorStack.TryPop(out oper) && oper.Type != TokenType.ParenOpen)
            output.Add(oper.Token);
        // the parenthesis is discarded intentionally (if the operator is not null)

        // if popping fails, there was no matching opening parenthesis before the bottom of the stack
        if (oper is null)
            throw ParserError($"No matching: {Tokens.PAREN_OPEN}");

        if (operatorStack.TryPeek(out var maybeFunction) && maybeFunction.Type == TokenType.MathFunction)
            output.Add(operatorStack.Pop().Token);
    }

    private static bool CheckUnary(List<Token> output, Token token, Token? prev)
    {
        switch ((prev ?? default).Type)
        {
            case TokenType.Number:
            case TokenType.Bool:
            case TokenType.Constant:
            case TokenType.Identifier:
            case TokenType.Parameter:
            case TokenType.Immutable:
            case TokenType.Persistent:
            case TokenType.Impersistent:
            case TokenType.Input:
            case TokenType.Output:
            case TokenType.ParenClose:
                return false;
        }

        output.Add(Tokens.GetReserved(Tokens.ZERO, token.Position));
        return true;
    }

    private void OnPrecedence(List<Token> output, Token token, bool unary = false)
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

    private void OnSeparator(List<Token> output)
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

    private void OnEmptyQueue(List<Token> output)
    {
        while (operatorStack.TryPop(out var oper))
        {
            Token token = oper.Token;
            if (token.Type == TokenType.ParenOpen)
                throw ParserError($"No matching: {Tokens.PAREN_CLOSE}");

            output.Add(token);
        }

        if (output.Count == 0)
            throw ParserError("Empty expression!");
    }

    #endregion

    #region Helpers

    private bool ResolveIdentifier(string name, out TokenType type)
    {
        bool resolved = false;
        if (functionLocalNames.Contains(name))
        {
            type = TokenType.FunctionLocal;
            resolved = true;
        }
        else if (declarationNames.TryGetValue(name, out type))
        {
            resolved = true;
        }
        return resolved;
    }

    private Token Expect(TokenType type)
    {
        Discard(type);
        return previousToken;
    }

    private void Discard(TokenType type)
    {
        if (!Accept(type))
            throw ParserError("Unexpected token!");
    }

    private bool Accept(TokenType type)
    {
        bool success = type == currentToken.Type;
        if (success)
            AdvanceToken();
        return success;
    }

    private bool Accept(TokenType type, out Token token)
    {
        bool success = Accept(type);
        if (success)
            token = previousToken;
        else
            token = default;
        return success;
    }

    private void AdvanceToken()
    {
        Debug.Assert(currentIndex <= maxIndex);
        if (currentIndex == maxIndex)
            throw ParserError("End reached unexpectedly!");

        previousToken = currentToken;
        currentToken = lexicalTokens[++currentIndex];
    }

    #endregion

    private ParserException ParserError(string error)
    {
        return new ParserException(error, currentToken);
    }
}
