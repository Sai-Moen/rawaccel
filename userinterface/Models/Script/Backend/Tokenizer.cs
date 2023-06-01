using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace userinterface.Models.Script.Backend
{
    /**
     * <summary>
     * 
     * <list type="table">
     * 
     * <listheader>
     * <term><see cref="TokenizerState"/></term>
     * <description>Phase of the Tokenizer, that determines the characters to accept.</description>
     * </listheader>
     * 
     * <item>
     * <term><see cref="Startup"/></term>
     * <description>Advance until the Parameters section starts, used for comments.</description>
     * </item>
     * 
     * <item>
     * <term><see cref="Parameters"/></term>
     * <description>Allows for up to 8 parameters to be declared.</description>
     * </item>
     * 
     * <item>
     * <term><see cref="Variables"/></term>
     * <description>Allows for variables to be declared.</description>
     * </item>
     * 
     * <item>
     * <term><see cref="Calculation"/></term>
     * <description>Represents one iteration of calculating the points.</description>
     * </item>
     * 
     * </list>
     * 
     * </summary>
     */
    internal enum TokenizerState
    {
        Startup,
        Parameters,
        Variables,
        Calculation,
    }

    /** <summary>
     * 
     * <see cref="Tokenizer"/>
     * Automatically attempts to Tokenize when given an input script.
     * 
     * </summary>
     */ 
    internal class Tokenizer
    {
        #region Constants

        private const int MaxParameters = 8;

        private const int MaxIdentifierLength = 0x10;

        private const int MaxLiteralLength = 0x20;

        #endregion Constants

        #region Fields

        private TokenizerState State = TokenizerState.Startup;

        private TokenType CurrentTokenState = TokenType.Undefined;

        private readonly StringBuilder TokenBuffer = new();

        internal IList<Token> TokenList { get; } = new List<Token>();

        private readonly TokenMap UsedIdentifiers = new Dictionary<string, Token>();

        private int CurrentIdx = -1;

        private readonly int MaxIdx;

        private char CurrentChar;

        private readonly char[] Characters;

        #endregion Fields

        #region Constructors

        internal Tokenizer(string script)
        {
            Characters = script
                .ReplaceLineEndings("")
                .Replace(" ", null)
                .Replace("\t", null)
                .ToCharArray();

            MaxIdx = Characters.Length - 1;

            CheckCharacters();

            Tokenize();
        }

        #endregion Constructors

        #region Methods

        private static bool IsAlphabeticCharacter(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }

        private static bool IsNumericCharacter(char c)
        {
            return (c >= '0' && c <= '9') || c == Tokens.Separators.FPOINT[0];
        }

        private bool IsReserved(char c)
        {
            return IsReserved(c.ToString());
        }

        private bool IsReserved(string s)
        {
            return UsedIdentifiers.ContainsKey(s) || Tokens.MapReserved.ContainsKey(s);
        }

        private void CheckCharacters()
        {
            // Not a problem in terms of parsing, but for consistency among (us) script writers.
            if (Characters[MaxIdx] != Tokens.Separators.CALC_END[0])
            {
                TokenizerError("Please don't type anything after the body.");
            }

            foreach (char c in Characters)
            {
                if (IsAlphabeticCharacter(c) || IsNumericCharacter(c) || IsReserved(c))
                {
                    continue;
                }

                TokenizerError($"Invalid character detected, char: {c}, u16: {(ushort)c}");
            }
        }

        private void Tokenize()
        {
            Debug.Assert(CurrentIdx == -1);
            while (++CurrentIdx <= MaxIdx)
            {
                CurrentChar = Characters[CurrentIdx];

                switch (State)
                {
                    case TokenizerState.Startup:
                        OnStateStartup();
                        break;
                    case TokenizerState.Parameters:
                        OnStateParameters();
                        break;
                    case TokenizerState.Variables:
                        OnStateVariables();
                        break;
                    case TokenizerState.Calculation:
                        OnStateCalculation();
                        break;
                }
            }
        }

        private void OnStateStartup()
        {
            if (StrCmpCurrentChar(Tokens.Separators.PARAMS_START))
            {
                State = TokenizerState.Parameters;
                Debug.Assert(State == TokenizerState.Parameters, "Paranoia");
            }
        }

        private void OnStateParameters()
        {
            if (StrCmpCurrentChar(Tokens.Separators.PARAMS_END))
            {
                AddTokenIfUnreserved();
                AddMapReservedCharacter();
                State = TokenizerState.Variables;
                Debug.Assert(State == TokenizerState.Variables, "Second line of defense (anti-side-effect)");
                return;
            }

            PreCalculationHelper(TokenType.Parameter);
        }

        private void OnStateVariables()
        {
            if (StrCmpCurrentChar(Tokens.Separators.CALC_START))
            {
                AddTokenIfUnreserved();
                AddMapReservedCharacter();
                State = TokenizerState.Calculation;
                Debug.Assert(State == TokenizerState.Calculation, "Second line of defense (anti-side-effect)");
                return;
            }

            PreCalculationHelper(TokenType.Variable);
        }

        private void OnStateCalculation()
        {
            if (StrCmpCurrentChar(Tokens.Separators.CALC_END))
            {
                AddTokenIfUnreserved();
                AddMapReservedCharacter();
                Debug.Assert(CurrentIdx == MaxIdx);
                return;
            }

            bool isAlphabetic = IsAlphabeticCharacter(CurrentChar);
            bool isNumeric = IsNumericCharacter(CurrentChar);
            bool isSeparator = Tokens.Separators.Set.Contains(CurrentChar);
            bool isOperator = Tokens.Operators.Set.Contains(CurrentChar);

            switch (CurrentTokenState)
            {
                case TokenType.Undefined:
                    BufferCurrentChar();

                    if (isAlphabetic)
                    {
                        CurrentTokenState = TokenType.Identifier;
                        return;
                    }
                    else if (isNumeric)
                    {
                        TokenizerError("Number literal not allowed during calculation!");
                    }
                    else if (isSeparator || isOperator)
                    {
                        if (isOperator && PeekNext() == Tokens.Operators.SECOND_C)
                        {
                            CurrentTokenState = TokenType.Operator;
                            return;
                        }

                        AddMapReservedToken();
                        return;
                    }

                    goto default;
                case TokenType.Identifier:
                    CapIdentifierLength();

                    if (isAlphabetic || isNumeric)
                    {
                        BufferCurrentChar();
                        return;
                    }
                    else if (isSeparator || isOperator)
                    {
                        AddAnyReservedToken();

                        if (isOperator && PeekNext() == Tokens.Operators.SECOND_C)
                        {
                            BufferCurrentChar();
                            CurrentTokenState = TokenType.Operator;
                            return;
                        }

                        AddMapReservedCharacter();
                        return;
                    }

                    goto default;
                case TokenType.Operator:
                    AddMapReservedToken();
                    return;
                default:
                    TokenizerError($"Indeterminate state during {State}!");
                    return;
            }
        }

        private void PreCalculationHelper(TokenType type)
        {
            bool isAlphabetic = IsAlphabeticCharacter(CurrentChar);
            bool isNumeric = IsNumericCharacter(CurrentChar);

            switch (CurrentTokenState)
            {
                case TokenType.Undefined:
                    BufferCurrentChar();

                    if (isAlphabetic)
                    {
                        if (type == TokenType.Parameter && UsedIdentifiers.Count > MaxParameters)
                        {
                            TokenizerError($"Too many parameters! (max {MaxParameters})");
                        }

                        CurrentTokenState = TokenType.Identifier;
                        return;
                    }
                    else if (isNumeric)
                    {
                        CurrentTokenState = TokenType.Literal;
                        return;
                    }

                    goto default;
                case TokenType.Identifier:
                    CapIdentifierLength();

                    if (isAlphabetic || isNumeric)
                    {
                        BufferCurrentChar();
                        return;
                    }

                    if (StrCmpCurrentChar(Tokens.Operators.ASSIGN))
                    {
                        CurrentTokenState = type;
                        Debug.Assert(CurrentTokenState == type, "Relies on side-effect");
                        AddTokenIfUnreserved();
                        AddMapReservedCharacter();
                        return;
                    }

                    goto default;
                case TokenType.Literal:
                    CapLiteralLength();

                    if (isAlphabetic)
                    {
                        TokenizerError("Unexpected letter in a number literal!");
                    }
                    else if (isNumeric)
                    {
                        BufferCurrentChar();
                        return;
                    }

                    if (StrCmpCurrentChar(Tokens.Separators.TERMINATOR))
                    {
                        AddTokenIfUnreserved();
                        AddMapReservedCharacter();
                        return;
                    }

                    goto default;
                default:
                    TokenizerError($"Indeterminate state during {State}!");
                    return;
            }
        }

        private void AddTokenIfUnreserved()
        {
            if (TokenBuffer.Length == 0)
            {
                Debug.Assert(CurrentTokenState == TokenType.Undefined, "Can't have a defined token without characters");
                return;
            }

            string s = TokenBuffer.ToString();

            if (IsReserved(s))
            {
                TokenizerError("Identifier reserved!");
            }

            Token token = new(CurrentTokenState, s);

            if (CurrentTokenState == TokenType.Parameter || CurrentTokenState == TokenType.Variable)
            {
                UsedIdentifiers.Add(s, token);
            }

            AddToken(token);
        }

        private void AddMapReservedCharacter()
        {
            AddMapReservedToken(CurrentChar.ToString());
        }

        private void AddMapReservedToken()
        {
            AddMapReservedToken(TokenBuffer.ToString());
        }

        private void AddMapReservedToken(string s)
        {
            Debug.Assert(!UsedIdentifiers.ContainsKey(s));

            Token token;

            if (Tokens.MapReserved.TryGetValue(s, out token!))
            {
                AddToken(token);
                return;
            }

            TokenizerError("Cannot add unmapped token!");
        }

        private void AddAnyReservedToken()
        {
            AddAnyReservedToken(TokenBuffer.ToString());
        }

        private void AddAnyReservedToken(string s)
        {
            Token token;

            if (Tokens.MapReserved.TryGetValue(s, out token!) ||
                UsedIdentifiers.TryGetValue(s, out token!))
            {
                AddToken(token);
                return;
            }

            TokenizerError("Cannot add unreserved token!");
        }

        private void AddToken(Token token)
        {
            TokenList.Add(token);
            TokenBuffer.Clear();
            CurrentTokenState = TokenType.Undefined;
        }

        private void BufferCurrentChar()
        {
            TokenBuffer.Append(CurrentChar);
        }

        private bool StrCmpCurrentChar(string s)
        {
            return CurrentChar == s[0];
        }

        private char PeekNext()
        {
            if (CurrentIdx < MaxIdx)
            {
                return Characters[CurrentIdx + 1];
            }

            TokenizerError("Tried to read beyond file bounds!");
            return char.MinValue;
        }

        private void CapIdentifierLength()
        {
            Debug.Assert(TokenBuffer.Length <= MaxIdentifierLength);
            if (TokenBuffer.Length == MaxIdentifierLength)
            {
                TokenizerError($"Identifier name too long! (stay below {MaxIdentifierLength} characters)");
            }
        }

        private void CapLiteralLength()
        {
            Debug.Assert(TokenBuffer.Length <= MaxLiteralLength);
            if (TokenBuffer.Length == MaxLiteralLength)
            {
                TokenizerError($"Number literal too long! (stay below {MaxLiteralLength} characters)");
            }
        }

        private void TokenizerError(string error)
        {
            throw new TokenizerException($"Index: {CurrentIdx}; " + error);
        }

        #endregion Methods
    }

    /// <summary>Ensures that we aren't accidentally catching unrelated exceptions while debugging.</summary>
    internal class TokenizerException : Exception
    {
        public TokenizerException()
            : base()
        {
        }

        public TokenizerException(string? message)
            : base(message)
        {
        }

        public TokenizerException(string? message, Exception? innerException)
            : base(message, innerException)
        {
        }
    }
}
