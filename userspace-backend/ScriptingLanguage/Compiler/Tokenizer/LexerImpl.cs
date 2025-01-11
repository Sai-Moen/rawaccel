using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace userspace_backend.ScriptingLanguage.Compiler.Tokenizer;

/// <summary>
/// Automatically attempts to Tokenize when given an input script.
/// </summary>
public class LexerImpl : ILexer
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

    protected enum CharBufferState
    {
        Idle,
        CommentLine,
        Identifier,
        Number,
        Special,

        Count
    }

    private CharBufferState bufferState = CharBufferState.Idle;
    private readonly StringBuilder charBuffer = new();
    private readonly List<TokenType> delimiterStack = [];

    private int baseIndex = -1;
    private int currentIndex;
    private readonly int maxIndex;

    private readonly string script;

    private string description = string.Empty;
    private readonly List<Token> lexicalTokens = [];
    private readonly List<string> symbolSideTable = [];

    /// <summary>
    /// Processes and tokenizes the input script.
    /// </summary>
    /// <param name="script">The input script.</param>
    public LexerImpl(string script)
    {
        this.script = script;
        maxIndex = this.script.Length - 1;
    }

    private static bool CmpCharStr(char c, string s) => c == s[0];

    private static bool IsAlphabeticCharacter(char c)
        => c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || CmpCharStr(c, Tokens.UNDERSCORE);

    private static bool IsNumericCharacter(char c)
        => c >= '0' && c <= '9' || CmpCharStr(c, Tokens.FPOINT);

    private static bool IsNewline(char c) => c == '\n';

    public LexingResult Tokenize()
    {
        for (currentIndex = 0; currentIndex <= maxIndex; currentIndex++)
        {
            if (CmpCharStr(script[currentIndex], Tokens.SQUARE_OPEN))
                break;
        }
        description = script[..currentIndex].Trim();

        baseIndex = currentIndex;
        if (currentIndex > maxIndex || !CmpCharStr(script[currentIndex], Tokens.SQUARE_OPEN))
        {
            throw LexerError("Parameters not found!");
        }

        for (; currentIndex <= maxIndex; currentIndex++)
        {
            char currentChar = script[currentIndex];

            LexerAction action;
            if (IsNewline(currentChar))
            {
                if (bufferState == CharBufferState.CommentLine)
                    bufferState = CharBufferState.Idle;

                action = LexerAction.None;
            }
            else if (bufferState == CharBufferState.CommentLine)
            {
                action = LexerAction.None;
            }
            else if (CmpCharStr(currentChar, Tokens.COMMENT_LINE))
            {
                bufferState = CharBufferState.CommentLine;
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
                    charBuffer.Append(currentChar);
                    switch (bufferState)
                    {
                        case CharBufferState.Idle:
                            bufferState = CharBufferState.Identifier;
                            baseIndex = currentIndex;
                            break;
                        case CharBufferState.Identifier:
                            CapIdentifierLength();
                            break;
                        case CharBufferState.Number:
                            throw LexerError("Letter detected inside number!");
                        default:
                            goto error;
                    }
                    break;
                case LexerAction.Numerical:
                    charBuffer.Append(currentChar);
                    switch (bufferState)
                    {
                        case CharBufferState.Idle:
                            bufferState = CharBufferState.Number;
                            baseIndex = currentIndex;
                            break;
                        case CharBufferState.Identifier:
                            CapIdentifierLength();
                            break;
                        case CharBufferState.Number:
                            CapNumberLength();
                            break;
                        default:
                            goto error;
                    }
                    break;
                case LexerAction.Whitespace:
                    switch (bufferState)
                    {
                        case CharBufferState.Idle:
                        case CharBufferState.CommentLine:
                            goto skip_whitespace;
                        case CharBufferState.Identifier:
                            AddBufferedPossiblyReservedSymbol();
                            break;
                        case CharBufferState.Number:
                            AddBufferedNumber();
                            break;
                        default:
                            goto error;
                    }
                    
                    bufferState = CharBufferState.Idle;

                skip_whitespace:
                    break;
                case LexerAction.Special:
                    switch (bufferState)
                    {
                        case CharBufferState.Idle:
                            break;
                        case CharBufferState.Identifier:
                            AddBufferedPossiblyReservedSymbol();
                            break;
                        case CharBufferState.Number:
                            AddBufferedNumber();
                            break;
                        case CharBufferState.Special:
                            Debug.Assert(CmpCharStr(currentChar, Tokens.EQUALS_SIGN));

                            charBuffer.Append(currentChar);
                            AddBufferedPossiblyReservedSymbol();
                            goto skip_special;
                        default:
                            goto error;
                    }

                    charBuffer.Append(currentChar);
                    if (PeekNext(out char c2) && CmpCharStr(c2, Tokens.EQUALS_SIGN))
                    {
                        bufferState = CharBufferState.Special;
                        baseIndex = currentIndex;
                        goto skip_special;
                    }

                    AddBufferedPossiblyReservedSymbol();

                    TokenType lastType = lexicalTokens[^1].Type;
                    if (delimiterStack.Count > 0)
                    {
                        // the special handling here is because the parameters section uses delimiters to denote bounds
                        TokenType bottom = delimiterStack[0];
                        if (bottom == TokenType.SquareOpen)
                        {
                            if (lastType == TokenType.SquareClose)
                                delimiterStack.RemoveAt(delimiterStack.Count - 1);

                            goto skip_special;
                        }
                    }

                    TokenType opposite;
                    switch (lastType)
                    {
                        case TokenType.ParenOpen:
                        case TokenType.SquareOpen:
                        case TokenType.CurlyOpen:
                            delimiterStack.Add(lastType);
                            goto skip_special;
                        case TokenType.ParenClose:
                            opposite = TokenType.ParenOpen;
                            break;
                        case TokenType.SquareClose:
                            opposite = TokenType.SquareOpen;
                            break;
                        case TokenType.CurlyClose:
                            opposite = TokenType.CurlyOpen;
                            break;
                        default:
                            goto skip_special;
                    }

                    if (delimiterStack.Count == 0 || delimiterStack[^1] != opposite)
                        throw LexerError("Unbalanced delimiters! Too many closing.");

                    delimiterStack.RemoveAt(delimiterStack.Count - 1);

                skip_special:
                    break;
                default:
                    goto error;
            }

            continue;

        error:
            throw LexerError("Undefined state!");
        }

        if (delimiterStack.Count > 0)
            throw LexerError("Unbalanced delimiters! Too many opening.");

        CompilerContext context = new(symbolSideTable);
        return new(context, description, lexicalTokens);
    }

    #region Helper Methods

    private void AddBufferedPossiblyReservedSymbol()
    {
        string symbol = ConsumeBuffer();

        Token token;
        if (Tokens.IsReserved(symbol))
        {
            token = Tokens.GetReserved(symbol, baseIndex);
        }
        else
        {
            SymbolIndex symbolIndex = AddSymbolToSideTable(Tokens.Normalize(symbol));
            token = new(TokenType.Identifier, baseIndex, symbolIndex);
        }
        AddToken(token);
    }

    private void AddBufferedNumber()
    {
        SymbolIndex symbolIndex = AddSymbolToSideTable(ConsumeBuffer());
        AddToken(new(TokenType.Number, baseIndex, symbolIndex));
    }

    private string ConsumeBuffer()
    {
        string s = charBuffer.ToString();
        charBuffer.Clear();
        return s;
    }

    private SymbolIndex AddSymbolToSideTable(string symbol)
    {
        SymbolIndex symbolIndex = (SymbolIndex)symbolSideTable.Count;
        symbolSideTable.Add(symbol);
        return symbolIndex;
    }

    private void AddToken(Token token)
    {
        lexicalTokens.Add(token);
        bufferState = CharBufferState.Idle;
    }

    private void CapIdentifierLength()
    {
        if (charBuffer.Length > Constants.MAX_IDENTIFIER_LEN)
            throw LexerError($"Identifier name too long! (max {Constants.MAX_IDENTIFIER_LEN} characters)");
    }

    private void CapNumberLength()
    {
        if (charBuffer.Length > Constants.MAX_NUMBER_LEN)
            throw LexerError($"Number too long! (max {Constants.MAX_NUMBER_LEN} characters)");
    }

    private bool PeekNext(out char c)
    {
        if (currentIndex < maxIndex)
        {
            c = script[currentIndex + 1];
            return true;
        }

        c = char.MinValue;
        return false;
    }

    #endregion

    private LexerException LexerError(string error)
    {
        return new LexerException(error, Tokens.DUMMY with { Position = baseIndex });
    }
}
