using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace userspace_backend.ScriptingLanguage.Lexing;

/// <summary>
/// State of the character buffer.
/// </summary>
internal enum CharBufferState
{
    Idle,
    CommentLine,
    Identifier,
    Number,
    Special,
}

/// <summary>
/// Automatically attempts to Tokenize when given an input script.
/// </summary>
public class Lexer : ILexer
{
    #region Fields

    private CharBufferState bufferState = CharBufferState.Idle;
    private readonly StringBuilder charBuffer = new();

    private int baseIndex = -1;
    private int currentIndex = -1;
    private readonly int maxIndex;

    private char currentChar;
    private readonly char[] characters;

    private string description = string.Empty;
    private readonly List<Token> lexicalTokens = [];
    private readonly List<string> symbolSideTable = [];

    #endregion

    #region Constructors

    /// <summary>
    /// Processes and tokenizes the input script.
    /// </summary>
    /// <param name="script">The input script.</param>
    public Lexer(string script)
    {
        characters = script.ToCharArray();
        maxIndex = characters.Length - 1;
    }

    #endregion

    #region Static Methods

    private static bool CmpCharStr(char c, string s) => c == s[0];

    private static bool IsAlphabeticCharacter(char c)
        => c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || CmpCharStr(c, Tokens.UNDERSCORE);

    private static bool IsNumericCharacter(char c)
        => c >= '0' && c <= '9' || CmpCharStr(c, Tokens.FPOINT);

    private static bool IsNewline(char c) => c == '\n';

    #endregion

    #region Methods

    public LexingResult Tokenize()
    {
        TokenizeDescription();
        TokenizeScript();
        return new(description, lexicalTokens, symbolSideTable);
    }

    private void TokenizeDescription()
    {
        StringBuilder builder = new(characters.Length);
        foreach (char c in characters)
        {
            if (CmpCharStr(c, Tokens.SQUARE_OPEN))
                break;

            currentIndex++;
            builder.Append(c);
        }
        baseIndex = currentIndex;
        description = builder.ToString().Trim();
    }

    private void TokenizeScript()
    {
        Debug.Assert(currentIndex <= maxIndex);
        if (currentIndex == maxIndex || !CmpCharStr(characters[currentIndex + 1], Tokens.SQUARE_OPEN))
        {
            throw LexerError("Parameters not found!");
        }

        while (++currentIndex <= maxIndex)
        {
            currentChar = characters[currentIndex];
            if (IsNewline(currentChar))
            {
                if (bufferState == CharBufferState.CommentLine)
                    bufferState = CharBufferState.Idle;

                continue;
            }
            else if (CommentCheck())
            {
                continue;
            }
            else if (IsAlphabeticCharacter(currentChar))
            {
                if (OnAlphabetical()) continue;
            }
            else if (IsNumericCharacter(currentChar))
            {
                if (OnNumerical()) continue;
            }
            else if (char.IsWhiteSpace(currentChar))
            {
                if (OnWhiteSpace()) continue;
            }
            else if (OnSpecial())
            {
                continue;
            }

            throw LexerError("Undefined state!");
        }
    }

    private bool CommentCheck()
    {
        bool isInComment;
        if (bufferState == CharBufferState.CommentLine)
        {
            isInComment = true;
        }
        else if (CmpCharStr(currentChar, Tokens.COMMENT_LINE))
        {
            _ = OnWhiteSpace();
            bufferState = CharBufferState.CommentLine;
            isInComment = true;
        }
        else
        {
            isInComment = false;
        }
        return isInComment;
    }

    private bool OnAlphabetical()
    {
        BufferCurrentChar();
        switch (bufferState)
        {
            case CharBufferState.Idle:
                bufferState = CharBufferState.Identifier;
                baseIndex = currentIndex;
                return true;
            case CharBufferState.Identifier:
                CapIdentifierLength();
                return true;
            case CharBufferState.Number:
                throw LexerError("Letter detected inside number!");
            default:
                return false;
        }
    }

    private bool OnNumerical()
    {
        BufferCurrentChar();
        switch (bufferState)
        {
            case CharBufferState.Idle:
                bufferState = CharBufferState.Number;
                baseIndex = currentIndex;
                return true;
            case CharBufferState.Identifier:
                CapIdentifierLength();
                return true;
            case CharBufferState.Number:
                CapNumberLength();
                return true;
            default:
                return false;
        }
    }

    private bool OnWhiteSpace()
    {
        switch (bufferState)
        {
            case CharBufferState.Idle:
                return true;
            case CharBufferState.Identifier:
                AddBufferedToken();
                break;
            case CharBufferState.Number:
                AddNumber();
                break;
            default:
                return false;
        }

        bufferState = CharBufferState.Idle;
        return true;
    }

    private bool OnSpecial()
    {
        switch (bufferState)
        {
            case CharBufferState.Idle:
                break;
            case CharBufferState.Identifier:
                AddBufferedToken();
                break;
            case CharBufferState.Number:
                AddNumber();
                break;
            case CharBufferState.Special:
                Debug.Assert(CmpCharStr(currentChar, Tokens.EQUALS_SIGN));

                BufferCurrentChar();
                AddBufferedToken();
                return true;
            default:
                return false;
        }

        BufferCurrentChar();
        if (PeekNext(out char c2) && CmpCharStr(c2, Tokens.EQUALS_SIGN))
        {
            bufferState = CharBufferState.Special;
            baseIndex = currentIndex;
        }
        else
        {
            AddBufferedToken();
        }

        return true;
    }

    #endregion

    #region Helper Methods

    private void AddToken(Token token)
    {
        lexicalTokens.Add(token);
        bufferState = CharBufferState.Idle;
    }

    private void AddBufferedToken()
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

    private void AddNumber()
    {
        SymbolIndex symbolIndex = AddSymbolToSideTable(ConsumeBuffer());
        AddToken(new(TokenType.Number, baseIndex, symbolIndex));
    }

    private SymbolIndex AddSymbolToSideTable(string symbol)
    {
        SymbolIndex symbolIndex = (SymbolIndex)symbolSideTable.Count;
        symbolSideTable.Add(symbol);
        return symbolIndex;
    }

    private void BufferCurrentChar()
    {
        charBuffer.Append(currentChar);
    }

    private string ConsumeBuffer()
    {
        string s = charBuffer.ToString();
        charBuffer.Clear();
        return s;
    }

    private bool PeekNext(out char c)
    {
        if (currentIndex < maxIndex)
        {
            c = characters[currentIndex + 1];
            return true;
        }

        c = char.MinValue;
        return false;
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

    #endregion

    private LexerException LexerError(string error)
    {
        return new LexerException(error, Tokens.DUMMY with { Position = baseIndex });
    }
}
