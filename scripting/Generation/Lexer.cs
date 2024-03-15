using System;
using System.Diagnostics;
using System.Text;

namespace scripting.Generation;

public enum CharBufferState
{
    Idle,
    Identifier,
    Number,
    Special,
}

/// <summary>
/// Automatically attempts to Tokenize when given an input script.
/// </summary>
public class Lexer
{
    #region Fields

    private CharBufferState BufferState = CharBufferState.Idle;
    private readonly StringBuilder CharBuffer = new();

    private int CurrentIndex = -1;
    private readonly int MaxIndex;

    private uint CurrentLine = 1;

    private char CurrentChar;
    private readonly char[] Characters;

    #endregion Fields

    #region Constructors

    /// <summary>
    /// Processes and tokenizes the input script.
    /// </summary>
    /// <param name="script">The input script.</param>
    public Lexer(string script)
    {
        Characters = script.ToCharArray();
        MaxIndex = Characters.Length - 1;
        CheckCharacters();
        Tokenize();
    }

    #endregion Constructors

    #region Properties

    public string Comments { get; private set; } = string.Empty;

    public TokenList TokenList { get; } = new();

    #endregion Properties

    #region Static Methods

    private static bool CmpCharStr(char c, string s) => c == s[0];

    private static bool IsReserved(char c) => IsReserved(c.ToString());
    private static bool IsReserved(string s) => Tokens.ReservedMap.ContainsKey(s);

    private static bool IsAlphabeticCharacter(char c)
        => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || CmpCharStr(c, Tokens.UNDER);

    private static bool IsNumericCharacter(char c)
        => (c >= '0' && c <= '9') || CmpCharStr(c, Tokens.FPOINT);

    private static bool IsNewline(char c) => c == Environment.NewLine[^1];

    #endregion Static Methods

    #region Methods

    private void CheckCharacters()
    {
        Debug.Assert(MaxIndex > 0, "MaxIdx not set correctly!");

        // not a problem in terms of parsing, but for consistency among (us) script writers
        if (!CmpCharStr(Characters[MaxIndex], Tokens.CALC_END))
        {
            CurrentLine = 0; // the location is in the error
            TokenizerError("Please don't type anything after the body.");
        }

        int startingIndex = -1;
        uint startingLine = 1;
        StringBuilder builder = new(Characters.Length);

        bool isComments = true;
        foreach (char c in Characters)
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
                CurrentLine++;
                continue;
            }
            else if (
                IsAlphabeticCharacter(c) ||
                IsNumericCharacter(c) ||
                char.IsWhiteSpace(c) ||
                IsReserved(c))
            {
                continue;
            }

            TokenizerError($"Unsupported character detected, char: {c}, u16: {(ushort)c}");
        }

        CurrentIndex = startingIndex - 1;
        CurrentLine = startingLine;
        Comments = builder.ToString();
    }

    private void Tokenize()
    {
        Debug.Assert(CmpCharStr(Characters[CurrentIndex + 1], Tokens.PARAMS_START),
            "Current Char should start at the parameter opening!");

        while (++CurrentIndex <= MaxIndex)
        {
            CurrentChar = Characters[CurrentIndex];
            if (IsNewline(CurrentChar))
            {
                CurrentLine++;
                continue;
            }
            else if (IsAlphabeticCharacter(CurrentChar))
            {
                if (OnAlphabetical()) continue;
            }
            else if (IsNumericCharacter(CurrentChar))
            {
                if (OnNumerical()) continue;
            }
            else if (char.IsWhiteSpace(CurrentChar))
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
        switch (BufferState)
        {
            case CharBufferState.Idle:
                BufferState = CharBufferState.Identifier;
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
        switch (BufferState)
        {
            case CharBufferState.Idle:
                BufferState = CharBufferState.Number;
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
        switch (BufferState)
        {
            case CharBufferState.Idle:
                return true;
            case CharBufferState.Identifier:
                AddBufferedToken();
                BufferState = CharBufferState.Idle;
                return true;
            case CharBufferState.Number:
                AddNumber();
                BufferState = CharBufferState.Idle;
                return true;
            default:
                return false;
        }
    }

    private bool OnSpecial()
    {
        switch (BufferState)
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
                Debug.Assert(CmpCharStr(CurrentChar, Tokens.SECOND));

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
            BufferState = CharBufferState.Special;
        }
        else
        {
            AddBufferedToken();
        }
    }

    #endregion Methods

    #region Helper Methods

    private void AddToken(Token token)
    {
        TokenList.Add(token);
        BufferState = CharBufferState.Idle;
    }

    private void AddBufferedToken()
    {
        string s = ConsumeBuffer();

        Token token;
        if (Tokens.ReservedMap.TryGetValue(s, out Token? value))
        {
            token = value with { Line = CurrentLine };
        }
        else
        {
            token = new(new(TokenType.Identifier, Tokens.Normalize(s)), CurrentLine);
        }
        AddToken(token);
    }

    private void AddNumber()
    {
        AddToken(new(new(TokenType.Number, ConsumeBuffer()), CurrentLine));
    }

    private void BufferCurrentChar()
    {
        CharBuffer.Append(CurrentChar);
    }

    private string ConsumeBuffer()
    {
        string s = CharBuffer.ToString();
        CharBuffer.Clear();
        return s;
    }

    private bool PeekNext(out char c)
    {
        c = char.MinValue;
        if (CurrentIndex < MaxIndex)
        {
            c = Characters[CurrentIndex + 1];
            return true;
        }

        return false;
    }

    private void CapIdentifierLength()
    {
        if (CharBuffer.Length > Constants.MAX_IDENTIFIER_LEN)
        {
            TokenizerError($"Identifier name too long! (max {Constants.MAX_IDENTIFIER_LEN} characters)");
        }
    }

    private void CapNumberLength()
    {
        if (CharBuffer.Length > Constants.MAX_NUMBER_LEN)
        {
            TokenizerError($"Number too long! (max {Constants.MAX_NUMBER_LEN} characters)");
        }
    }

    #endregion Helper Methods

    private void TokenizerError(string error)
    {
        throw new LexerException(error, CurrentLine);
    }
}

/// <summary>
/// Exception for tokenizing-specific errors.
/// </summary>
public sealed class LexerException : GenerationException
{
    public LexerException(string message, uint line) : base(message, line) { }
}
