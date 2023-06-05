using System.Diagnostics;
using System.Text;

namespace userinterface.Models.Script.Generation
{
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
    internal class Tokenizer
    {
        #region Constants

        public const int MaxParameters = 8;

        public const int MaxIdentifierLength = 0x10;

        public const int MaxNumberLength = 0x20;

        public const char NewLine = '\n';

        #endregion Constants

        #region Fields

        internal TokenList TokenList { get; } = new();

        private readonly StringBuilder CharBuffer = new();

        private CharBufferState BufferState = CharBufferState.Idle;

        private int CurrentIdx = -1;

        private readonly int MaxIdx;

        private uint CurrentLine = 1;

        private char CurrentChar;

        private readonly char[] Characters;

        #endregion Fields

        #region Constructors

        internal Tokenizer(string script)
        {
            Characters = script
                .ReplaceLineEndings(NewLine.ToString())
                .Replace(" ", null)
                .Replace("\t", null)
                .ToCharArray();

            MaxIdx = Characters.Length - 1;

            CheckCharacters();

            Tokenize();
        }

        #endregion Constructors

        #region Static Methods

        private static bool CmpCharStr(char c, string s)
        {
            return c == s[0];
        }

        private static bool IsReserved(char c)
        {
            return IsReserved(c.ToString());
        }

        private static bool IsReserved(string s)
        {
            return Tokens.ReservedMap.ContainsKey(s);
        }

        private static bool IsAlphabeticCharacter(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }

        private static bool IsNumericCharacter(char c)
        {
            return (c >= '0' && c <= '9') || CmpCharStr(c, Tokens.FPOINT);
        }

        #endregion Static Methods

        #region Methods

        private void CheckCharacters()
        {
            Debug.Assert(MaxIdx > 0, "MaxIdx not set correctly!");

            // Not a problem in terms of parsing, but for consistency among (us) script writers.
            if (!CmpCharStr(Characters[MaxIdx], Tokens.CALC_END))
            {
                CurrentLine = 0; // The location is in the error.
                TokenizerError("Please don't type anything after the body.");
            }

            int startingIndex = 0;
            uint startingLine = 1;
            bool isComments = true;

            foreach (char c in Characters)
            {
                if (isComments)
                {
                    startingLine += (uint)(c == NewLine ? 1 : 0);
                    isComments ^= CmpCharStr(c, Tokens.PARAMS_START);
                    startingIndex = isComments ? ++startingIndex : --startingIndex;
                    continue;
                }
                else if (c == NewLine)
                {
                    ++CurrentLine;
                    continue;
                }
                else if (IsAlphabeticCharacter(c) || IsNumericCharacter(c) || IsReserved(c))
                {
                    continue;
                }

                TokenizerError($"Unsupported character detected, char: {c}, u16: {(ushort)c}");
            }

            CurrentIdx = startingIndex;
            CurrentLine = startingLine;
        }

        private void Tokenize()
        {
            Debug.Assert(CmpCharStr(Characters[CurrentIdx + 1], Tokens.PARAMS_START),
                "Current Char should start at the parameter opening!");
            while (++CurrentIdx <= MaxIdx)
            {
                CurrentChar = Characters[CurrentIdx];

                if (CurrentChar == NewLine)
                {
                    ++CurrentLine;
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
                    goto default;
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

        private bool OnSpecial()
        {
            switch (BufferState)
            {
                case CharBufferState.Idle: // No buffer, so one character?
                    if (PeekNext(out char c))
                    {
                        Debug.Assert(c != Tokens.SECOND);
                    }
                    BufferCurrentChar();
                    AddBufferedToken();
                    return true;
                case CharBufferState.Identifier:
                    AddBufferedToken();
                    goto SpecialCheck;
                case CharBufferState.Number:
                    AddNumber();
                    goto SpecialCheck;
                SpecialCheck:
                    BufferCurrentChar();
                    if (PeekNext(out c) && c == Tokens.SECOND)
                    {
                        BufferState = CharBufferState.Special;
                    }
                    else
                    {
                        AddBufferedToken();
                    }
                    return true;
                case CharBufferState.Special:
                    Debug.Assert(CurrentChar == Tokens.SECOND);
                    BufferCurrentChar();
                    AddBufferedToken();
                    return true;
                default:
                    return false;
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
                token = new(new(TokenType.Identifier, s), CurrentLine);
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

            if (CurrentIdx < MaxIdx)
            {
                c = Characters[CurrentIdx + 1];
                return true;
            }
            return false;
        }

        private void CapIdentifierLength()
        {
            if (CharBuffer.Length > MaxIdentifierLength)
            {
                TokenizerError($"Identifier name too long! (max {MaxIdentifierLength} characters)");
            }
        }

        private void CapNumberLength()
        {
            if (CharBuffer.Length > MaxNumberLength)
            {
                TokenizerError($"Number too long! (max {MaxNumberLength} characters)");
            }
        }

        #endregion Helper Methods

        private void TokenizerError(string error)
        {
            throw new TokenizerException(CurrentLine, error);
        }
    }

    public class TokenizerException : TranspilerException
    {
        public TokenizerException(uint line, string message) : base($"Line {line}: {message}") {}
    }
}
