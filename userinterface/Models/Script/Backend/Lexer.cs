using System;
using System.Collections.Generic;
using System.Text;

namespace userinterface.Models.Script.Backend
{
    internal enum LexerState
    {
        Comments,
        Parameters,
        Variables,
        Calculation,
    }

    internal class Lexer
    {
        #region Fields

        private LexerState State = LexerState.Comments;

        private TokenType CurrentTokenState = TokenType.Undefined;

        private const int TokenBufferSize = 0x10;

        private readonly StringBuilder TokenBuffer = new(TokenBufferSize);

        private readonly IList<Token> TokenList = new List<Token>();

        private int CurrentIdx = -1;

        private readonly int MaxIdx;

        private int CurrentLine = 1;

        private int CurrentColumn = 1;

        private static readonly ISet<string> TokenSet = new HashSet<string>(Tokens.AllTokens);

        private readonly char[] Characters;

        #endregion Fields

        #region Constructors

        internal Lexer(string script)
        {
            Characters = script.Trim().ToCharArray();

            MaxIdx = Characters.Length - 1;

            CheckCharacters();

            Tokenize();
        }

        #endregion Constructors

        #region Methods

        private static bool IsWhitespaceCharacter(char c)
        {
            return c == ' ' || c == '\t' || c == '\n' || c == '\r';
        }

        private static bool IsAlphabeticCharacter(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }

        private static bool IsNumericCharacter(char c)
        {
            return c >= '0' && c <= '9';
        }

        private static bool IsAllowedCharacter(char c)
        {
            return IsWhitespaceCharacter(c) || IsAlphabeticCharacter(c) || IsNumericCharacter(c);
        }

        private void CheckCharacters()
        {
            foreach (char c in Characters)
            {
                if (IsAllowedCharacter(c) || TokenSet.Contains(c.ToString()))
                {
                    ++CurrentColumn;
                    if (c == '\n')
                    {
                        ++CurrentLine;
                        CurrentColumn = 0;
                    }
                    continue;
                }

                throw new Exception(
                    Location() +
                    $"Invalid character detected, char: {c}, codepoint16: {(short)c}");
            }

            CurrentLine = 1;
            CurrentColumn = 1;
        }

        private void Tokenize()
        {
            while (++CurrentIdx <= MaxIdx)
            {
                Update();

                switch (State)
                {
                    case LexerState.Comments:
                        OnStateComments();
                        break;
                    case LexerState.Parameters:
                        OnStateParameters();
                        break;
                    case LexerState.Variables:
                        OnStateVariables();
                        break;
                    case LexerState.Calculation:
                        OnStateCalculation();
                        break;
                }
            }
        }

        private void Update()
        {
            if (GetCurrentChar() == '\n')
            {
                ++CurrentLine;
                CurrentColumn = 0;
            }
            else
            {
                ++CurrentColumn;
            }
        }

        private void OnStateComments()
        {
            if (GetCurrentString() == Tokens.Separators.PARAMS_START)
            {
                State = LexerState.Parameters;
            }
        }

        private void OnStateParameters()
        {
            if (GetCurrentString() == Tokens.Separators.PARAMS_END)
            {
                State = LexerState.Variables;
                return;
            }

            char currentc = GetCurrentChar();
            if (IsWhitespaceCharacter(currentc)) return;

            char? _next = PeekNext();
            if (_next == null) return;
            char next = _next.Value;

            if (IsAlphabeticCharacter(currentc))
            {
                if (TokenBuffer.Length >= TokenBufferSize)
                {
                    throw new Exception(Location() + $"Parameter name too long! (max {TokenBufferSize})");
                }

                switch (CurrentTokenState)
                {
                    case TokenType.Undefined:
                    case TokenType.Parameter:
                        CurrentTokenState = TokenType.Parameter;
                        TokenBuffer.Append(currentc);
                        break;
                    case TokenType.Literal:
                        throw new Exception(Location() + "Unexpected letter in a number literal!");
                    default:
                        throw new Exception(Location() + "Invalid state!");
                }

                if (IsWhitespaceCharacter(next) || next.ToString() == Tokens.Operators.ASSIGNMENT)
                {
                    TokenList.Add(new Token(TokenBuffer.ToString(), CurrentTokenState));
                    TokenBuffer.Clear();
                    CurrentTokenState = TokenType.Undefined;
                    return;
                }
            }

            //TODO: numeric literals

            string currents = GetCurrentString();

            switch (currents)
            {
                case Tokens.Operators.ASSIGNMENT:
                    TokenList.Add(new Token(currents, Tokens.Operators.DefaultTokenType));
                    return;
                case Tokens.Separators.TERMINATOR:
                    TokenList.Add(new Token(currents, Tokens.Separators.DefaultTokenType));
                    return;
            }

            throw new Exception(Location() + "Indeterminate state!");
        }

        private void OnStateVariables()
        {
            if (GetCurrentString() == Tokens.Separators.CALC_START)
            {
                State = LexerState.Calculation;
                return;
            }

        }

        private void OnStateCalculation()
        {
            if (GetCurrentString() == Tokens.Separators.CALC_END)
            {
                CurrentIdx = MaxIdx; // Done
                return;
            }

        }

        private char GetCurrentChar()
        {
            return Characters[CurrentIdx];
        }

        private string GetCurrentString()
        {
            return Characters[CurrentIdx].ToString();
        }

        private char? PeekNext()
        {
            if (CurrentIdx == MaxIdx) return null;
            return Characters[CurrentIdx + 1];
        }

        private string Location()
        {
            return $"L{CurrentLine}:C{CurrentColumn}; ";
        }

        #endregion Methods
    }
}
