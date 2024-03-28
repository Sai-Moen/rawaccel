using scripting.Common;
using System.Diagnostics;
using System.Text;

namespace scripting.Lexical;

enum CharBufferState
{
    Idle,
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

    private string comments = string.Empty;
    private readonly ITokenList lexicalTokens = [];

    #endregion

    #region Constructors

    /// <summary>
    /// Processes and tokenizes the input script.
    /// </summary>
    /// <param name="script">The input script.</param>
    public Lexer(string script)
    {
        characters = script.Trim().ToCharArray();
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
        CheckCharacters();
        TokenizeScript();
        return new(comments, lexicalTokens);
    }

    private void CheckCharacters()
    {
        Debug.Assert(maxIndex > 0, "MaxIdx not set correctly!");

        // not a problem in terms of parsing, but for consistency among (us) script writers
        if (!CmpCharStr(characters[maxIndex], Tokens.CALC_END))
        {
            currentLine = 0; // the location is in the error (setting this for side-effect (sorry (not sorry)))
            TokenizerError("Please don't type anything after the calculation section.");
        }

        int startingIndex = -1;
        uint startingLine = 1;
        StringBuilder builder = new(characters.Length);

        bool isComments = true;
        foreach (char c in characters)
        {
            if (isComments)
            {
                startingIndex++;
                if (IsNewline(c)) startingLine++;
                builder.Append(c);

                isComments ^= CmpCharStr(c, Tokens.PARAMS_START);
                continue;
            }
            else if (IsNewline(c))
            {
                currentLine++;
                continue;
            }
            else if (
                IsAlphabeticCharacter(c) ||
                IsNumericCharacter(c) ||
                char.IsWhiteSpace(c) ||
                Tokens.IsReserved(c))
            {
                continue;
            }

            TokenizerError($"Unsupported character detected, char: {c}, u16: {(ushort)c}");
        }

        currentIndex = startingIndex - 1;
        currentLine = startingLine;
        comments = builder.Remove(builder.Length - 1, 1).ToString();
    }

    private void TokenizeScript()
    {
        Debug.Assert(CmpCharStr(characters[currentIndex + 1], Tokens.PARAMS_START),
            "Current Char should start at the parameter opening!");

        while (++currentIndex <= maxIndex)
        {
            currentChar = characters[currentIndex];
            if (IsNewline(currentChar))
            {
                currentLine++;
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
            else // Must be a reserved token, or error.
            {
                if (OnSpecial()) continue;
            }

            TokenizerError("Undefined state!");
        }
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
                TokenizerError("Letter detected inside number!");
                return false;
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
            TokenizerError($"Identifier name too long! (max {Constants.MAX_IDENTIFIER_LEN} characters)");
        }
    }

    private void CapNumberLength()
    {
        if (charBuffer.Length > Constants.MAX_NUMBER_LEN)
        {
            TokenizerError($"Number too long! (max {Constants.MAX_NUMBER_LEN} characters)");
        }
    }

    #endregion

    private void TokenizerError(string error)
    {
        throw new LexerException(error, currentLine);
    }
}
