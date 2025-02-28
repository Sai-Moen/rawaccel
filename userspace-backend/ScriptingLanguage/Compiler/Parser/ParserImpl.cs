using System.Collections.Generic;
using System.Diagnostics;
using userspace_backend.ScriptingLanguage.Compiler.Tokenizer;
using userspace_backend.ScriptingLanguage.Script;

namespace userspace_backend.ScriptingLanguage.Compiler.Parser;

/// <summary>
/// Parses a stream of tokens.
/// </summary>
public class ParserImpl(CompilerContext context, ILexer lexer) : IParser
{
    private readonly CompilerContext context = context;
    private readonly ILexer lexer = lexer;

    private Token previousToken;
    private Token currentToken;

    private readonly Dictionary<string, TokenType> declarationNames = new(Constants.MAX_MEM_CAP);
    private readonly HashSet<string> functionLocalNames = [];

    // keeps track of amount of recursion, could also consider using explicit stack
    private int depth;

    private readonly Parameters parameters = [];
    private readonly Block declarations = [];
    private readonly Dictionary<string, ParsedCallback> callbacks = [];

    public AST Parse()
    {
        // init currentToken
        AdvanceToken();

        string description = context.GetSymbol(Expect(TokenType.Description));

        Discard(TokenType.SquareOpen);
        while (currentToken.Type != TokenType.SquareClose)
            ParseParameter();
        Discard(TokenType.SquareClose);

        while (currentToken.Type != TokenType.CurlyOpen)
            ParseDeclaration();

        Discard(TokenType.CurlyOpen);
        Block asts = [];
        while (currentToken.Type != TokenType.CurlyClose)
            asts.Add(Statement());
        callbacks.Add(Calculation.NAME, new(Calculation.NAME, [], [.. asts]));
        Discard(TokenType.CurlyClose);

        while (Accept(TokenType.Identifier, out Token identifier))
        {
            List<Token> args = [];
            if (Accept(TokenType.ParenOpen))
                args = Expression(TokenType.ParenClose, TokenType.CurlyOpen);

            Block code = ParseBlock();

            string symbol = context.GetSymbol(identifier);
            ParsedCallback callback = new(symbol, [.. args], [.. code]);
            if (callbacks.ContainsKey(callback.Name))
                throw ParserError("Duplicate callbacks detected!");

            callbacks[callback.Name] = callback;
        }

        if (parameters.Count > Constants.MAX_PARAMETERS)
            throw ParserError(
                $"Too many parameters! Expected at most {Constants.MAX_PARAMETERS}, got {parameters.Count}.");

        // this can be expanded a bit more since persistent and impersistent are separated, but for now it'll work
        if (declarations.Count > Constants.MAX_DECLARATIONS)
            throw ParserError(
                $"Too many declarations! Expected at most {Constants.MAX_DECLARATIONS}, got {declarations.Count}.");

        return new(description, parameters, declarations, [.. callbacks.Values]);
    }

    public void Reset()
    {
        context.Reset();
        lexer.Reset();

        previousToken = default;
        currentToken = default;

        declarationNames.Clear();
        functionLocalNames.Clear();

        depth = 0;

        parameters.Clear();
        declarations.Clear();
        callbacks.Clear();
    }

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

    private void ParseDeclaration()
    {
        Token declarer = currentToken;
        TokenType type = declarer.MapDeclarer();
        if (type == TokenType.None)
            throw ParserError("Unknown declarer!");
        
        // couldn't use Expect due to mapping, so we have to advance manually
        AdvanceToken();

        Token identifier = Expect(TokenType.Identifier) with { Type = type };
        string symbol = context.GetSymbol(identifier);
        if (declarationNames.ContainsKey(symbol))
            throw ParserError($"Name collision! Name {symbol} already exists.");

        ASTTag tag;
        ASTUnion union;
        if (type == TokenType.Function)
        {
            List<Token> args = [];
            if (Accept(TokenType.ParenOpen) && !Accept(TokenType.ParenClose))
            {
                do
                {
                    if (!Accept(TokenType.Identifier, out Token arg))
                        throw ParserError("User-defined functions can only have identifier arguments.");

                    functionLocalNames.Add(context.GetSymbol(arg));
                    args.Add(arg);
                }
                while (Accept(TokenType.ArgumentSeparator));
                Discard(TokenType.ParenClose);
            }

            Block code = ParseBlock();
            functionLocalNames.Clear();

            tag = ASTTag.Function;
            union = new()
            {
                astFunction = new(identifier, [.. args], [.. code])
            };
        }
        else
        {
            Token eq = Expect(TokenType.Assignment);

            List<Token> output = Expression(before: TokenType.Terminator);

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

    private Block ParseBlock()
    {
        if (++depth > Constants.MAX_RECURSION_DEPTH)
            throw ParserError("Exceeded Maximum Recursion Depth!");

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

            if (!Accept(TokenType.Assignment, out Token assignment))
                assignment = Expect(TokenType.Compound);

            List<Token> initializer = Expression(before: TokenType.Terminator);

            tag = ASTTag.Assign;
            union = new()
            {
                astAssign = new(target, assignment, [.. initializer])
            };
        }
        else if (Accept(TokenType.If))
        {
            List<Token> condition = Expression(after: TokenType.CurlyOpen);

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
            List<Token> condition = Expression(after: TokenType.CurlyOpen);

            Block whileBlock = ParseBlock();

            tag = ASTTag.While;
            union = new()
            {
                astWhile = new([.. condition], [.. whileBlock])
            };
        }
        else if (Accept(TokenType.Return))
        {
            List<Token> expression;
            if (Accept(TokenType.Terminator))
                expression = [];
            else
                expression = Expression(before: TokenType.Terminator);

            tag = ASTTag.Return;
            union = new()
            {
                astReturn = new([.. expression])
            };
        }
        else
        {
            throw ParserError($"Expected statement!");
        }

        return new(tag, union);
    }

    /// <summary>
    /// Possible ways to call it:
    /// <br/>
    /// 1. Only set 'before'
    /// <br/>
    ///     This causes the parser to parse an expression up to, but not including the given type.
    ///     The token that made it stop will be in previousToken (so it automatically gets Discarded).
    /// <br/>
    /// 2. Only set 'after'
    /// <br/>
    ///     This causes the parser to parse an expression up to, but not including the given type.
    ///     The token that made it stop will be in currentToken (so it can be used with Accept/Expect).
    /// <br/>
    /// 3. Set both arguments
    /// <br/>
    ///     This causes the parser to parse an expression until it sees both given types consecutively.
    ///     The tokens corresponding to 'before' and 'after' will be in previousToken and currentToken, respectively.
    ///     This is useful for e.g. when a closing parenthesis is not part of an expression,
    ///     but you want the curly brace after it to end up in currentToken, instead of the parenthesis.
    /// </summary>
    private List<Token> Expression(TokenType before = TokenType.None, TokenType after = TokenType.None)
    {
        bool ignoreBefore = before == TokenType.None;
        bool ignoreAfter  = after  == TokenType.None;
        Debug.Assert(
            !(ignoreBefore && ignoreAfter),
            $"Expression must receive at least {nameof(before)} or {nameof(after)} in order to know when to stop.");

        Stack<Operator> operatorStack = [];
        List<Token> expression = [];

        // initializing prev as default, since it's the previous token in the expression, not the whole parser
        // the condition is just an extreme case failsafe where all tokens are somehow exhausted (otherwise infinite loop)
        for (Token prev = default; previousToken.Type != TokenType.None; prev = previousToken)
        {
            Token token = currentToken;
            AdvanceToken();

            bool matchesBefore = before == previousToken.Type;
            bool matchesAfter  = after  == currentToken.Type;

            if (matchesBefore && (ignoreAfter || matchesAfter))
                break;

            switch (token.Type)
            {
                case TokenType.Number:
                case TokenType.Bool:
                case TokenType.Constant:
                case TokenType.Input:
                case TokenType.Output:
                    expression.Add(token);
                    break;
                case TokenType.Identifier:
                    {
                        string name = context.GetSymbol(token);
                        if (!ResolveIdentifier(name, out TokenType type))
                            throw ParserError($"Could not resolve name! (name was: {name})", token);

                        Token resolved = token with { Type = type };
                        if (type == TokenType.Function)
                            operatorStack.Push(new(resolved, -1));
                        else
                            expression.Add(resolved);
                    }
                    break;
                case TokenType.Arithmetic:
                    {
                        bool unary;
                        switch (prev.Type)
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
                                unary = false;
                                break;
                            default:
                                // cursed way to handle unary operators
                                expression.Add(Tokens.GetReserved(Tokens.ZERO, token.BytePosition));
                                unary = true;
                                break;
                        }
                        HandlePrecedences(operatorStack, expression, token, unary);
                    }
                    break;
                case TokenType.Comparison:
                    HandlePrecedences(operatorStack, expression, token);
                    break;
                case TokenType.ArgumentSeparator:
                    while (operatorStack.TryPop(out var oper))
                    {
                        if (oper.Type == TokenType.ParenOpen)
                        {
                            operatorStack.Push(oper);
                            goto found_paren_open;
                        }

                        expression.Add(oper.Token);
                    }

                    // if popping fails, this was just a random separator
                    throw ParserError($"Unexpected: {Tokens.ARG_SEP}", token);

                found_paren_open:
                    break;
                case TokenType.MathFunction:
                    operatorStack.Push(new(token, -1));
                    break;
                case TokenType.ParenOpen:
                    operatorStack.Push(new(token, -1));
                    break;
                case TokenType.ParenClose:
                    {
                        Operator oper;
                        while (operatorStack.TryPop(out oper) && oper.Type != TokenType.ParenOpen)
                            expression.Add(oper.Token);
                        // the parenthesis is discarded intentionally (if the operator is not null)

                        // if popping fails, there was no matching opening parenthesis before the bottom of the stack
                        if (oper.Type == TokenType.None)
                            throw ParserError($"No matching: {Tokens.PAREN_OPEN}", token);

                        if (operatorStack.TryPeek(out var maybeFunction) && maybeFunction.Type.IsFunction())
                        {
                            Operator fun = operatorStack.Pop();
                            expression.Add(fun.Token);
                        }
                    }
                    break;
                default:
                    throw ParserError("Unexpected expression token!", token);
            }

            if (matchesAfter && (ignoreBefore || matchesBefore))
                break;
        }

        while (operatorStack.TryPop(out var oper))
        {
            Token token = oper.Token;
            if (token.Type == TokenType.ParenOpen)
                throw ParserError($"No matching: {Tokens.PAREN_CLOSE}", token);

            expression.Add(token);
        }

        if (expression.Count == 0)
            throw ParserError("Empty expression!");

        return expression;
    }

    private static void HandlePrecedences(Stack<Operator> operatorStack, List<Token> expression, Token token, bool unary = false)
    {
        Operator tokenOperator = new(token, token.Precedence(unary));
        bool left = token.LeftAssociative();
        while (operatorStack.TryPop(out var oper))
        {
            if (oper.HasHigherPrecedence(tokenOperator, left))
            {
                expression.Add(oper.Token);
            }
            else
            {
                operatorStack.Push(oper);
                break;
            }
        }
        operatorStack.Push(tokenOperator);
    }

    #region Helpers

    private bool ResolveIdentifier(string name, out TokenType type)
    {
        bool ok;
        if (functionLocalNames.Contains(name))
        {
            type = TokenType.FunctionLocal;
            ok = true;
        }
        else
        {
            ok = declarationNames.TryGetValue(name, out type);
        }
        return ok;
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
        bool ok = type == currentToken.Type;
        if (ok)
            AdvanceToken();
        return ok;
    }

    private bool Accept(TokenType type, out Token token)
    {
        bool ok = Accept(type);
        token = ok ? previousToken : default;
        return ok;
    }

    private void AdvanceToken()
    {
        previousToken = currentToken;
        currentToken = lexer.Advance();
    }

    #endregion

    private ParserException ParserError(string error)
    {
        return ParserError(error, currentToken);
    }

    private static ParserException ParserError(string error, Token suspect)
    {
        return new ParserException(error, suspect);
    }
}
