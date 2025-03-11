using System.Collections.Generic;
using System.Diagnostics;

namespace userspace_backend.ScriptingLanguage.Compiler;

/// <summary>
/// Exception for parsing-related errors.
/// </summary>
public sealed class ParserException(string message, Token suspect)
    : CompilationException(message, suspect)
{ }

internal readonly record struct Operator(Token Token, int Precedence)
{
    internal TokenKind Kind => Token.Kind;

    internal bool HasHigherPrecedence(Operator other, bool left)
        => Kind.HasPrecedence() &&
            (Precedence > other.Precedence || left && Precedence == other.Precedence);
}

/// <summary>
/// Parses a stream of tokens.
/// </summary>
public class Parser(Context context, Lexer lexer)
{
    private readonly Context context = context;
    private readonly Lexer lexer = lexer;

    private Token previousToken;
    private Token currentToken;

    private readonly Dictionary<string, TokenKind> declarationNames = new(Constants.MAX_MEM_CAP);
    private readonly HashSet<string> functionLocalNames = [];

    // keeps track of amount of recursion, could also consider using explicit stack
    private int depth;

    private readonly Parameters parameters = [];
    private readonly Block declarations = [];
    private readonly Dictionary<string, ParsedCallback> callbacks = [];

    public AST Parse()
    {
        // init currentToken
        Advance();

        string description = context.GetSymbol(Expect(TokenKind.Description));

        #region Parse Parameters

        Discard(TokenKind.SquareOpen);
        while (!Accept(TokenKind.SquareClose))
        {
            Token identifier = Expect(TokenKind.Identifier) with { Kind = TokenKind.Parameter };
            string symbol = context.GetSymbol(identifier);
            if (!declarationNames.TryAdd(symbol, TokenKind.Parameter))
                throw ParserError($"Name collision! Name {symbol} already exists.");

            Discard(TokenKind.Assignment);

            Token value = currentToken;
            ParameterValidation minval = default, maxval = default;
            switch (value.Kind)
            {
                case TokenKind.Bool:
                    Advance();
                    break;
                case TokenKind.Number:
                    Advance();

                    bool boundsParsed = false;

                    Token lower;
                    switch (currentToken.Kind)
                    {
                        case TokenKind.ParenOpen:
                            Advance();
                            lower = Expect(TokenKind.Number);
                            minval = new(Bound.LowerExcl, Number.Parse(context.GetSymbol(lower), lower));
                            break;
                        case TokenKind.SquareOpen:
                            Advance();
                            lower = Expect(TokenKind.Number);
                            minval = new(Bound.LowerIncl, Number.Parse(context.GetSymbol(lower), lower));
                            break;
                        case TokenKind.CurlyOpen:
                            Advance();
                            break;
                        default:
                            boundsParsed = true;
                            break;
                    }

                    if (boundsParsed || Accept(TokenKind.CurlyClose))
                        break;

                    if (Accept(TokenKind.ArgumentSeparator))
                    {
                        if (minval.Type == Bound.None)
                            throw ParserError($"Number between '{Tokens.CURLY_OPEN}' and '{Tokens.ARG_SEP}'!");
                    }
                    else
                    {
                        if (minval.Type != Bound.None)
                            throw ParserError("Expected a separator and number for upper bound!");
                    }

                    Token upper = Expect(TokenKind.Number);
                    switch (currentToken.Kind)
                    {
                        case TokenKind.ParenClose:
                            Advance();
                            maxval = new(Bound.UpperIncl, Number.Parse(context.GetSymbol(upper), upper));
                            break;
                        case TokenKind.SquareClose:
                            Advance();
                            maxval = new(Bound.UpperExcl, Number.Parse(context.GetSymbol(upper), upper));
                            break;
                        case TokenKind.CurlyClose:
                            throw ParserError($"Unexpected number attached to infinite upper bound!");
                        default:
                            throw ParserError($"Unexpected upper bound token!");
                    }

                    break;
                default:
                    throw ParserError("Expected either a boolean or numeric value for the parameter!");
            }

            Discard(TokenKind.Terminator);
            parameters.Add(new(context, identifier, value, minval, maxval));
        }

        #endregion

        #region Parse Declarations

        while (!Peek(TokenKind.CurlyOpen))
        {
            TokenKind kind = currentToken.MapDeclarer();
            if (kind == TokenKind.None)
                throw ParserError("Unknown declarer!");
        
            // couldn't use Expect due to mapping, so we have to advance manually
            Advance();

            Token identifier = Expect(TokenKind.Identifier) with { Kind = kind };
            string symbol = context.GetSymbol(identifier);
            if (declarationNames.ContainsKey(symbol))
                throw ParserError($"Name collision! Name {symbol} already exists.");

            ASTTag tag;
            ASTUnion union;
            if (kind == TokenKind.Function)
            {
                List<Token> args = [];
                if (Accept(TokenKind.ParenOpen) && !Accept(TokenKind.ParenClose))
                {
                    do
                    {
                        if (!Accept(TokenKind.Identifier, out Token arg))
                            throw ParserError("User-defined functions can only have identifier arguments.");

                        functionLocalNames.Add(context.GetSymbol(arg));
                        args.Add(arg);
                    }
                    while (Accept(TokenKind.ArgumentSeparator));
                    Discard(TokenKind.ParenClose);
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
                Token eq = Expect(TokenKind.Assignment);

                List<Token> output = Expression(before: TokenKind.Terminator);

                tag = ASTTag.Assign;
                union = new()
                {
                    astAssign = new(identifier, eq, [.. output])
                };
            }
            declarations.Add(new ASTNode(tag, union));

            // this is done last to avoid having to check for circular dependencies on this variable
            declarationNames.Add(symbol, kind);
        }

        #endregion

        #region Parse Callbacks

        Block asts = ParseBlock();
        callbacks.Add(Calculation.NAME, new(Calculation.NAME, [], [.. asts]));

        while (Accept(TokenKind.Identifier, out Token identifier))
        {
            List<Token> args = [];
            if (Accept(TokenKind.ParenOpen))
                args = Expression(TokenKind.ParenClose, TokenKind.CurlyOpen);

            Block code = ParseBlock();

            string symbol = context.GetSymbol(identifier);
            ParsedCallback callback = new(symbol, [.. args], [.. code]);
            if (callbacks.ContainsKey(callback.Name))
                throw ParserError("Duplicate callbacks detected!");

            callbacks[callback.Name] = callback;
        }

        #endregion

        #region Checks

        if (parameters.Count > Constants.MAX_PARAMETERS)
            throw ParserError(
                $"Too many parameters! Expected at most {Constants.MAX_PARAMETERS}, got {parameters.Count}.");

        // this can be expanded a bit more since persistent and impersistent are separated, but for now it'll work
        if (declarations.Count > Constants.MAX_DECLARATIONS)
            throw ParserError(
                $"Too many declarations! Expected at most {Constants.MAX_DECLARATIONS}, got {declarations.Count}.");

        foreach (ASTNode node in declarations)
        {
            if (node.Tag != ASTTag.Assign)
                continue;

            foreach (Token token in node.Union.astAssign.Initializer)
            {
                switch (token.Kind)
                {
                    case TokenKind.Input:
                        throw ParserError($"Cannot use '{Tokens.INPUT}' outside of functions!", token);
                    case TokenKind.Output:
                        throw ParserError($"Cannot use '{Tokens.OUTPUT}' outside of functions!", token);
                    case TokenKind.Comparison:
                        throw ParserError("Cannot use comparison operators outside of conditions!", token);
                }
            }
        }

        #endregion

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

    private Block ParseBlock()
    {
        if (++depth > Constants.MAX_RECURSION_DEPTH)
            throw ParserError("Exceeded Maximum Recursion Depth!");

        Block asts = [];
        Discard(TokenKind.CurlyOpen);
        while (!Accept(TokenKind.CurlyClose))
            asts.Add(Statement());

        --depth;
        return asts;
    }

    private ASTNode Statement()
    {
        ASTTag tag;
        ASTUnion union;

        bool isAssignment =
            Accept(TokenKind.Identifier) ||
            Accept(TokenKind.Input) ||
            Accept(TokenKind.Output);
        if (isAssignment)
        {
            Token target = previousToken;
            if (target.Kind == TokenKind.Identifier)
            {
                string name = context.GetSymbol(target);
                if (!ResolveIdentifier(name, out TokenKind kind))
                    throw ParserError($"Unknown assignment target! {name} has not been declared.");

                switch (kind)
                {
                    case TokenKind.Parameter:
                        throw ParserError("Cannot assign to parameter!");
                    case TokenKind.Immutable:
                        throw ParserError("Cannot assign to immutable variable!");
                    case TokenKind.Persistent:
                    case TokenKind.Impersistent:
                    case TokenKind.FunctionLocal:
                        break;
                    default:
                        Debug.Fail("Unreachable: got an unknown TokenKind...");
                        break;
                }

                target = target with { Kind = kind };
            }

            if (!Accept(TokenKind.Assignment, out Token assignment))
                assignment = Expect(TokenKind.Compound);

            List<Token> initializer = Expression(before: TokenKind.Terminator);

            tag = ASTTag.Assign;
            union = new()
            {
                astAssign = new(target, assignment, [.. initializer])
            };
        }
        else if (Accept(TokenKind.If))
        {
            List<Token> condition = Expression(after: TokenKind.CurlyOpen);

            Block ifBlock = ParseBlock();

            Block elseBlock;
            if (Accept(TokenKind.Else))
                elseBlock = ParseBlock();
            else
                elseBlock = [];

            tag = ASTTag.If;
            union = new()
            {
                astIf = new([.. condition], [.. ifBlock], [.. elseBlock])
            };
        }
        else if (Accept(TokenKind.While))
        {
            List<Token> condition = Expression(after: TokenKind.CurlyOpen);

            Block whileBlock = ParseBlock();

            tag = ASTTag.While;
            union = new()
            {
                astWhile = new([.. condition], [.. whileBlock])
            };
        }
        else if (Accept(TokenKind.Return))
        {
            List<Token> expression;
            if (Accept(TokenKind.Terminator))
                expression = [];
            else
                expression = Expression(before: TokenKind.Terminator);

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
    ///     This causes the parser to parse an expression up to, but not including the given kind.
    ///     The token that made it stop will be in previousToken (so it automatically gets Discarded).
    /// <br/>
    /// 2. Only set 'after'
    /// <br/>
    ///     This causes the parser to parse an expression up to, but not including the given kind.
    ///     The token that made it stop will be in currentToken (so it can be used with Accept/Expect).
    /// <br/>
    /// 3. Set both arguments
    /// <br/>
    ///     This causes the parser to parse an expression until it sees both given kinds consecutively.
    ///     The tokens corresponding to 'before' and 'after' will be in previousToken and currentToken, respectively.
    ///     This is useful for e.g. when a closing parenthesis is not part of an expression,
    ///     but you want the curly brace after it to end up in currentToken, instead of the parenthesis.
    /// </summary>
    private List<Token> Expression(TokenKind before = TokenKind.None, TokenKind after = TokenKind.None)
    {
        bool ignoreBefore = before == TokenKind.None;
        bool ignoreAfter  = after  == TokenKind.None;
        Debug.Assert(
            !(ignoreBefore && ignoreAfter),
            $"Expression must receive at least {nameof(before)} or {nameof(after)} in order to know when to stop.");

        Stack<Operator> operatorStack = [];
        List<Token> expression = [];

        // initializing prev as default, since it's the previous token in the expression, not the whole parser
        // the condition is just an extreme case failsafe where all tokens are somehow exhausted (otherwise infinite loop)
        for (Token prev = default; previousToken.Kind != TokenKind.None; prev = previousToken)
        {
            Token token = currentToken;
            Advance();

            bool matchesBefore = before == previousToken.Kind;
            bool matchesAfter  = after  == currentToken.Kind;

            if (matchesBefore && (ignoreAfter || matchesAfter))
                break;

            switch (token.Kind)
            {
                case TokenKind.Number:
                case TokenKind.Bool:
                case TokenKind.Constant:
                case TokenKind.Input:
                case TokenKind.Output:
                    expression.Add(token);
                    break;
                case TokenKind.Identifier:
                    {
                        string name = context.GetSymbol(token);
                        if (!ResolveIdentifier(name, out TokenKind kind))
                            throw ParserError($"Could not resolve name! (name was: {name})", token);

                        Token resolved = token with { Kind = kind };
                        if (kind == TokenKind.Function)
                            operatorStack.Push(new(resolved, -1));
                        else
                            expression.Add(resolved);
                    }
                    break;
                case TokenKind.Arithmetic:
                    {
                        bool unary;
                        switch (prev.Kind)
                        {
                            case TokenKind.Number:
                            case TokenKind.Bool:
                            case TokenKind.Constant:
                            case TokenKind.Identifier:
                            case TokenKind.Parameter:
                            case TokenKind.Immutable:
                            case TokenKind.Persistent:
                            case TokenKind.Impersistent:
                            case TokenKind.Input:
                            case TokenKind.Output:
                            case TokenKind.ParenClose:
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
                case TokenKind.Comparison:
                    HandlePrecedences(operatorStack, expression, token);
                    break;
                case TokenKind.ArgumentSeparator:
                    while (operatorStack.TryPop(out var oper))
                    {
                        if (oper.Kind == TokenKind.ParenOpen)
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
                case TokenKind.MathFunction:
                    operatorStack.Push(new(token, -1));
                    break;
                case TokenKind.ParenOpen:
                    operatorStack.Push(new(token, -1));
                    break;
                case TokenKind.ParenClose:
                    {
                        Operator oper;
                        while (operatorStack.TryPop(out oper) && oper.Kind != TokenKind.ParenOpen)
                            expression.Add(oper.Token);
                        // the parenthesis is discarded intentionally (if the operator is not null)

                        // if popping fails, there was no matching opening parenthesis before the bottom of the stack
                        if (oper.Kind == TokenKind.None)
                            throw ParserError($"No matching: {Tokens.PAREN_OPEN}", token);

                        if (operatorStack.TryPeek(out var maybeFunction) && maybeFunction.Kind.IsFunction())
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
            if (token.Kind == TokenKind.ParenOpen)
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

    private bool ResolveIdentifier(string name, out TokenKind kind)
    {
        bool ok;
        if (functionLocalNames.Contains(name))
        {
            kind = TokenKind.FunctionLocal;
            ok = true;
        }
        else
        {
            ok = declarationNames.TryGetValue(name, out kind);
        }
        return ok;
    }

    private Token Expect(TokenKind kind)
    {
        Discard(kind);
        return previousToken;
    }

    private void Discard(TokenKind kind)
    {
        if (!Accept(kind))
            throw ParserError("Unexpected token!");
    }

    private bool Accept(TokenKind kind)
    {
        bool ok = Peek(kind);
        if (ok)
            Advance();
        return ok;
    }

    private bool Accept(TokenKind kind, out Token token)
    {
        bool ok = Accept(kind);
        token = ok ? previousToken : default;
        return ok;
    }

    private bool Peek(TokenKind kind)
    {
        return kind == currentToken.Kind;
    }

    private void Advance()
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
