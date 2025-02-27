using System;
using System.Diagnostics;

namespace userspace_backend.ScriptingLanguage.Compiler.Tokenizer;

/// <summary>
/// Tokenizes an input script.
/// </summary>
public class LexerImpl(CompilerContext context) : ILexer
{
    protected enum LexerAction
    {
        None,

        Alphabetical,
        Numerical,
        Whitespace,
        Special,

        Count
    }

    protected enum CharViewState
    {
        Description, // must be the default value

        Idle,
        CommentLine,
        Identifier,
        Number,
        Special,

        Count
    }

    private readonly CompilerContext context = context;

    private CharViewState charViewState;

    private int tokenBegin;
    private int currentIndex;

    private string Script => context.Script;

    private static bool CmpCharStr(char c, string s) => c == s[0];

    private static bool IsAlphabeticCharacter(char c)
        => c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || CmpCharStr(c, Tokens.UNDERSCORE);

    private static bool IsNumericCharacter(char c)
        => c >= '0' && c <= '9' || CmpCharStr(c, Tokens.FPOINT);

    private static bool IsNewline(char c) => c == '\n';

    public Token Advance()
    {
        Debug.Assert(currentIndex >= 0);
        if (currentIndex >= Script.Length)
            return Tokens.DUMMY;

        int tokenLength = 0;
        if (charViewState == CharViewState.Description)
        {
            int tokenEnd = 0;
            for (; currentIndex < Script.Length; currentIndex++)
            {
                char currentChar = Script[currentIndex];
                if (CmpCharStr(currentChar, Tokens.SQUARE_OPEN))
                    goto params_found;

                if (!char.IsWhiteSpace(currentChar))
                {
                    tokenBegin = currentIndex;
                    tokenEnd = currentIndex;
                    break;
                }
            }

            for (; currentIndex < Script.Length; currentIndex++)
            {
                char currentChar = Script[currentIndex];
                if (CmpCharStr(currentChar, Tokens.SQUARE_OPEN))
                    goto params_found;

                if (!char.IsWhiteSpace(currentChar))
                    tokenEnd = currentIndex;
            }

            throw LexerError("Could not find parameters!");

        params_found:
            tokenLength = tokenEnd - tokenBegin + 1;
            SymbolIndex symbolIndex = context.AddSymbol(ConsumeBuffer(tokenLength));
            return new(TokenType.Description, tokenBegin, symbolIndex);
        }

        for (; currentIndex < Script.Length; currentIndex++)
        {
            char currentChar = Script[currentIndex];

            LexerAction action;
            if (IsNewline(currentChar))
            {
                if (charViewState == CharViewState.CommentLine)
                    charViewState = CharViewState.Idle;

                action = LexerAction.None;
            }
            else if (charViewState == CharViewState.CommentLine)
            {
                action = LexerAction.None;
            }
            else if (CmpCharStr(currentChar, Tokens.COMMENT_LINE))
            {
                charViewState = CharViewState.CommentLine;
                action = LexerAction.Whitespace;
            }
            else if (IsAlphabeticCharacter(currentChar))
            {
                action = LexerAction.Alphabetical;
            }
            else if (IsNumericCharacter(currentChar))
            {
                action = LexerAction.Numerical;
            }
            else if (char.IsWhiteSpace(currentChar))
            {
                action = LexerAction.Whitespace;
            }
            else
            {
                action = LexerAction.Special;
            }

            switch (action)
            {
                case LexerAction.None:
                    break;
                case LexerAction.Alphabetical:
                    ++tokenLength;
                    switch (charViewState)
                    {
                        case CharViewState.Idle:
                            charViewState = CharViewState.Identifier;
                            tokenBegin = currentIndex;
                            break;
                        case CharViewState.Identifier:
                            CapIdentifierLength(tokenLength);
                            break;
                        case CharViewState.Number:
                            throw LexerError("Letter detected inside number!");
                        default:
                            goto error;
                    }
                    break;
                case LexerAction.Numerical:
                    ++tokenLength;
                    switch (charViewState)
                    {
                        case CharViewState.Idle:
                            charViewState = CharViewState.Number;
                            tokenBegin = currentIndex;
                            break;
                        case CharViewState.Identifier:
                            CapIdentifierLength(tokenLength);
                            break;
                        case CharViewState.Number:
                            CapNumberLength(tokenLength);
                            break;
                        default:
                            goto error;
                    }
                    break;
                case LexerAction.Whitespace:
                    switch (charViewState)
                    {
                        case CharViewState.Idle:
                        case CharViewState.CommentLine:
                            break;
                        case CharViewState.Identifier:
                            return ConsumeBufferedSymbol(tokenLength);
                        case CharViewState.Number:
                            return ConsumeBufferedNumber(tokenLength);
                        default:
                            goto error;
                    }
                    break;
                case LexerAction.Special:
                    switch (charViewState)
                    {
                        case CharViewState.Idle:
                            tokenBegin = currentIndex;
                            break;
                        case CharViewState.Identifier:
                            return ConsumeBufferedSymbol(tokenLength);
                        case CharViewState.Number:
                            return ConsumeBufferedNumber(tokenLength);
                        case CharViewState.Special:
                            Debug.Assert(CmpCharStr(currentChar, Tokens.EQUALS_SIGN));
                            break;
                        default:
                            goto error;
                    }

                    ++tokenLength;
                    if (charViewState != CharViewState.Special && PeekNext(out char c2) && CmpCharStr(c2, Tokens.EQUALS_SIGN))
                    {
                        charViewState = CharViewState.Special;
                    }
                    else
                    {
                        ++currentIndex; // force increment because return skips it otherwise
                        return ConsumeBufferedSymbol(tokenLength);
                    }

                    break;
                default:
                    goto error;
            }
        }

    error:
        throw LexerError("Undefined state!");
    }

    public void Reset()
    {
        context.Reset();

        charViewState = CharViewState.Description;

        tokenBegin = 0;
        currentIndex = 0;
    }

    private Token ConsumeBufferedSymbol(int tokenLength)
    {
        ReadOnlyMemory<char> charView = ConsumeBuffer(tokenLength);

        Token token;
        if (Tokens.IsReserved(charView.Span))
        {
            token = Tokens.GetReserved(charView.Span, tokenBegin);
        }
        else
        {
            SymbolIndex symbolIndex = context.AddSymbol(charView);
            token = new(TokenType.Identifier, tokenBegin, symbolIndex);
        }
        return token;
    }

    private Token ConsumeBufferedNumber(int tokenLength)
    {
        SymbolIndex symbolIndex = context.AddSymbol(ConsumeBuffer(tokenLength));
        return new(TokenType.Number, tokenBegin, symbolIndex);
    }

    private ReadOnlyMemory<char> ConsumeBuffer(int tokenLength)
    {
        ReadOnlyMemory<char> charView = Script.AsMemory(tokenBegin, tokenLength);
        charViewState = CharViewState.Idle;
        return charView;
    }

    private void CapIdentifierLength(int tokenLength)
    {
        if (tokenLength > Constants.MAX_IDENTIFIER_LEN)
            throw LexerError($"Identifier name too long! (max {Constants.MAX_IDENTIFIER_LEN} characters)");
    }

    private void CapNumberLength(int tokenLength)
    {
        if (tokenLength > Constants.MAX_NUMBER_LEN)
            throw LexerError($"Number too long! (max {Constants.MAX_NUMBER_LEN} characters)");
    }

    private bool PeekNext(out char c)
    {
        int nextIndex = currentIndex + 1;
        bool ok = nextIndex < Script.Length;
        c = ok ? Script[nextIndex] : '\0';
        return ok;
    }

    private LexerException LexerError(string error)
    {
        return new LexerException(error, Tokens.DUMMY with { BytePosition = tokenBegin });
    }
}
