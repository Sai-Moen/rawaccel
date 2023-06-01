using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace userinterface.Models.Script.Backend
{
    /**
     * <summary>
     * Phase of the Tokenizer, that determines the characters and tokens to accept.
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

        public const int MaxParameters = 8;

        public const int MaxIdentifierLength = 0x10;

        public const int MaxLiteralLength = 0x20;

        public const char NewLine = '\n';

        #endregion Constants

        #region Fields

        private TokenizerState State = TokenizerState.Startup;

        private TokenType CurrentTokenState = TokenType.Undefined;

        private readonly StringBuilder TokenBuffer = new();

        internal IList<Token> TokenList { get; } = new List<Token>();

        private readonly IDictionary<string, Token> UsedIdentifiers = new Dictionary<string, Token>();

        private int CurrentIdx = -1;

        private readonly int MaxIdx;

        private int CurrentLine = 1;

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

            Debug.Assert(CurrentLine == 1, "CurrentLine not set correctly!");

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
            Debug.Assert(MaxIdx > 0, "MaxIdx not set correctly!");

            // Not a problem in terms of parsing, but for consistency among (us) script writers.
            if (Characters[MaxIdx] != Tokens.Separators.CALC_END[0])
            {
                CurrentLine = -1; // The location is in the error.
                TokenizerError("Please don't type anything after the body.");
            }

            bool isComments = true;

            foreach (char c in Characters)
            {
                if (isComments)
                {
                    isComments = c != Tokens.Separators.PARAMS_START[0];
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

            CurrentLine = 1;
        }

        private void Tokenize()
        {
            Debug.Assert(CurrentIdx == -1, "CurrentIdx not initialized correctly!");
            while (++CurrentIdx <= MaxIdx)
            {
                CurrentChar = Characters[CurrentIdx];

                if (CurrentChar == NewLine)
                {
                    ++CurrentLine;
                    continue;
                }

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
            if (StringCompareCurrentChar(Tokens.Separators.PARAMS_START))
            {
                AddMapReservedCharacter();
                State = TokenizerState.Parameters;
            }
        }

        private void OnStateParameters()
        {
            if (StringCompareCurrentChar(Tokens.Separators.PARAMS_END))
            {
                AddTokenIfUnreserved();
                AddMapReservedCharacter();
                State = TokenizerState.Variables;
                return;
            }

            PreCalculationHelper(TokenType.Parameter);
        }

        private void OnStateVariables()
        {
            if (StringCompareCurrentChar(Tokens.Separators.CALC_START))
            {
                AddTokenIfUnreserved();
                AddMapReservedCharacter();
                State = TokenizerState.Calculation;
                return;
            }

            PreCalculationHelper(TokenType.Variable);
        }

        private void OnStateCalculation()
        {
            if (StringCompareCurrentChar(Tokens.Separators.CALC_END))
            {
                AddAnyReservedToken();
                AddMapReservedCharacter();
                Debug.Assert(CurrentIdx == MaxIdx, "Final character check got removed?");
                return;
            }

            bool isAlphabetic = IsAlphabeticCharacter(CurrentChar);
            bool isNumeric = IsNumericCharacter(CurrentChar);
            bool isSeparator = Tokens.Separators.Set.Contains(CurrentChar);
            bool isOperator = Tokens.Operators.Set.Contains(CurrentChar);

            bool isTwoCharOperator = isOperator && PeekNext() == Tokens.Operators.SECOND_C;

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
                        if (isTwoCharOperator)
                        {
                            CurrentTokenState = TokenType.Operator;
                            return;
                        }

                        AddMapReservedToken();
                        return;
                    }

                    break;
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

                        if (isTwoCharOperator)
                        {
                            BufferCurrentChar();
                            CurrentTokenState = TokenType.Operator;
                            return;
                        }

                        AddMapReservedCharacter();
                        return;
                    }

                    break;
                case TokenType.Operator:
                    AddMapReservedToken();
                    return;
            }

            TokenizerError($"Indeterminate state during {State}!");
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

                    break;
                case TokenType.Identifier:
                    CapIdentifierLength();

                    if (isAlphabetic || isNumeric)
                    {
                        BufferCurrentChar();
                        return;
                    }

                    if (StringCompareCurrentChar(Tokens.Operators.ASSIGN))
                    {
                        CurrentTokenState = type;
                        Debug.Assert(CurrentTokenState == type, "Relies on side-effect");
                        AddTokenIfUnreserved();
                        AddMapReservedCharacter();
                        return;
                    }

                    break;
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

                    if (StringCompareCurrentChar(Tokens.Separators.TERMINATOR))
                    {
                        AddTokenIfUnreserved();
                        AddMapReservedCharacter();
                        return;
                    }

                    break;
            }

            TokenizerError($"Indeterminate state during {State}!");
        }

        private void AddTokenIfUnreserved()
        {
            Debug.Assert(State != TokenizerState.Calculation, "Only declare new Identifiers before Calculation!");

            if (TokenBuffer.Length == 0)
            {
                Debug.Assert(CurrentTokenState == TokenType.Undefined);
                return;
            }

            string s = TokenBuffer.ToString();

            if (IsReserved(s))
            {
                TokenizerError("Identifier reserved!");
            }

            Token token = new(CurrentTokenState, s);

            if (CurrentTokenState != TokenType.Literal)
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
            Debug.Assert(TokenBuffer.Length != 0, "Can't add empty reserved token!");
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
            if (TokenBuffer.Length == 0)
            {
                Debug.Assert(CurrentTokenState == TokenType.Undefined);
                return;
            }
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

        private bool StringCompareCurrentChar(string s)
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
            return char.MinValue; // born to throw, forced to return
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
            throw new TokenizerException($"Line {CurrentLine}: {error}");
        }

        #endregion Methods
    }

    public class TokenizerException : TranspilerException
    {
        public TokenizerException(string message)
            : base(message)
        {
        }
    }
}
