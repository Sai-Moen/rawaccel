using scripting.Common;
using System.Diagnostics;
using System.Text;

namespace scripting.Lexing;

enum CharBufferState
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

    private int currentIndex = -1;
    private readonly int maxIndex;

    private uint currentLine = 1;

    private char currentChar;
    private readonly char[] characters;

    private string description = string.Empty;
    private readonly ITokenList lexicalTokens = [];

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
        => c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || CmpCharStr(c, Tokens.UNDER);

    private static bool IsNumericCharacter(char c)
        => c >= '0' && c <= '9' || CmpCharStr(c, Tokens.FPOINT);

    private static bool IsNewline(char c) => c == '\n';

    #endregion

    #region Methods

    public LexingResult Tokenize()
    {
        TokenizeDescription();
        TokenizeScript();
        return new(description, lexicalTokens);
    }

    private void TokenizeDescription()
    {
        StringBuilder builder = new(characters.Length);
        foreach (char c in characters)
        {
            if (CmpCharStr(c, Tokens.SQUARE_OPEN))
            {
                break;
            }
            else if (IsNewline(c))
            {
                currentLine++;
            }
            currentIndex++;
            builder.Append(c);
        }
        description = builder.ToString().Trim();
    }

    private void TokenizeScript()
    {
        if (!CmpCharStr(characters[currentIndex + 1], Tokens.SQUARE_OPEN))
        {
            throw LexerError("Parameters not found!");
        }

        while (++currentIndex <= maxIndex)
        {
            currentChar = characters[currentIndex];
            if (IsNewline(currentChar))
            {
                currentLine++;
                if (bufferState == CharBufferState.CommentLine)
                {
                    bufferState = CharBufferState.Idle;
                }
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
                bufferState = CharBufferState.Idle;
                return true;
            case CharBufferState.Number:
                AddNumber();
                bufferState = CharBufferState.Idle;
                return true;
            default:
                return false;
        }
    }

    private bool OnSpecial()
    {
        switch (bufferState)
        {
            case CharBufferState.Idle:
                SpecialCheck();
                return true;
            case CharBufferState.Identifier:
                AddBufferedToken();
                SpecialCheck();
                return true;
            case CharBufferState.Number:
                AddNumber();
                SpecialCheck();
                return true;
            case CharBufferState.Special:
                Debug.Assert(CmpCharStr(currentChar, Tokens.SECOND));

                BufferCurrentChar();
                AddBufferedToken();
                return true;
            default:
                return false;
        }
    }

    private void SpecialCheck()
    {
        BufferCurrentChar();
        if (PeekNext(out char c2) && CmpCharStr(c2, Tokens.SECOND))
        {
            bufferState = CharBufferState.Special;
        }
        else AddBufferedToken();
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
            token = Tokens.GetReserved(symbol, currentLine);
        }
        else
        {
            token = new(new(TokenType.Identifier, Tokens.Normalize(symbol)), currentLine);
        }
        AddToken(token);
    }

    private void AddNumber()
    {
        AddToken(new(new(TokenType.Number, ConsumeBuffer()), currentLine));
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
        {
            throw LexerError($"Identifier name too long! (max {Constants.MAX_IDENTIFIER_LEN} characters)");
        }
    }

    private void CapNumberLength()
    {
        if (charBuffer.Length > Constants.MAX_NUMBER_LEN)
        {
            throw LexerError($"Number too long! (max {Constants.MAX_NUMBER_LEN} characters)");
        }
    }

    #endregion

    private LexerException LexerError(string error)
    {
        return new LexerException(error, currentLine);
    }
}
