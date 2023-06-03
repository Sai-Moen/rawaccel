using System.Diagnostics;
using System.Text;

namespace userinterface.Models.Script.Generation
{
    /// <summary>
    /// Phase of the Tokenizer, that determines the characters and tokens to accept.
    /// </summary>
    internal enum TokenizerState
    {
        Comments,
        Parameters,
        Variables,
        Calculation,
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

        private TokenizerState State = TokenizerState.Comments;

        private TokenType CurrentTokenState = TokenType.Undefined;

        internal TokenList TokenList { get; } = new();

        private readonly TokenMap UsedIdentifiers = new();

        private readonly StringBuilder CharBuffer = new();

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
            return UsedIdentifiers.ContainsKey(s) || Tokens.ReservedMap.ContainsKey(s);
        }

        private void CheckCharacters()
        {
            Debug.Assert(MaxIdx > 0, "MaxIdx not set correctly!");

            // Not a problem in terms of parsing, but for consistency among (us) script writers.
            if (Characters[MaxIdx] != Tokens.Separators.CALC_END[0])
            {
                CurrentLine = 0; // The location is in the error.
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
                    case TokenizerState.Comments:
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
                AddReservedCharacter();
                State = TokenizerState.Parameters;
            }
        }

        private void OnStateParameters()
        {
            if (StringCompareCurrentChar(Tokens.Separators.PARAMS_END))
            {
                AddTokenIfUnused();
                AddReservedCharacter();
                State = TokenizerState.Variables;
                return;
            }

            PreCalculationHelper(TokenType.Parameter);
        }

        private void OnStateVariables()
        {
            if (StringCompareCurrentChar(Tokens.Separators.CALC_START))
            {
                AddTokenIfUnused();
                AddReservedCharacter();
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
                AddReservedCharacter();
                Debug.Assert(CurrentIdx == MaxIdx, "Final character check got removed?");
                return;
            }

            bool isAlphabetic = IsAlphabeticCharacter(CurrentChar);
            bool isNumeric = IsNumericCharacter(CurrentChar);

            bool isSeparator = Tokens.Separators.CalcSet.Contains(CurrentChar);
            bool isOperator = Tokens.Operators.FullSet.Contains(CurrentChar);

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
                        TokenizerError("Number not allowed during calculation!");
                    }
                    else if (isSeparator || isOperator)
                    {
                        if (isTwoCharOperator)
                        {
                            CurrentTokenState = TokenType.Operator;
                            return;
                        }

                        AddReservedToken();
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

                        AddReservedCharacter();
                        return;
                    }

                    break;
                case TokenType.Operator:
                    BufferCurrentChar();
                    AddReservedToken();
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
                        CurrentTokenState = TokenType.Number;
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
                        AddTokenIfUnused();
                        AddReservedCharacter();
                        return;
                    }

                    if (type == TokenType.Variable)
                    {
                        // Allow the user to assign a Parameter to a Variable
                        if (StringCompareCurrentChar(Tokens.Separators.TERMINATOR))
                        {
                            AddUsedToken();
                            AddReservedCharacter();
                            return;
                        }
                    }

                    break;
                case TokenType.Number:
                    CapNumberLength();

                    if (isAlphabetic)
                    {
                        TokenizerError("Unexpected letter in a number!");
                    }
                    else if (isNumeric)
                    {
                        BufferCurrentChar();
                        return;
                    }

                    if (StringCompareCurrentChar(Tokens.Separators.TERMINATOR))
                    {
                        AddTokenIfUnused();
                        AddReservedCharacter();
                        return;
                    }
                    
                    break;
            }

            TokenizerError($"Indeterminate state during {State}!");
        }

        private void AddTokenIfUnused()
        {
            Debug.Assert(State != TokenizerState.Calculation, "Only declare new Identifiers before Calculation!");

            if (CharBuffer.Length == 0)
            {
                Debug.Assert(CurrentTokenState == TokenType.Undefined);
                return;
            }

            string s = CharBuffer.ToString();

            if (IsReserved(s))
            {
                TokenizerError("Identifier reserved!");
            }

            Token token = new(CurrentTokenState, CurrentLine, s);

            Debug.Assert(CurrentTokenState == TokenType.Number ||
                (CurrentTokenState & (TokenType.Parameter | TokenType.Variable)) != TokenType.Undefined,
                "Invalid state while trying to reserve identifier!");
            if (CurrentTokenState != TokenType.Number)
            {
                UsedIdentifiers.Add(s, token);
            }

            AddToken(token);
        }

        private void AddReservedCharacter()
        {
            AddReservedToken(CurrentChar.ToString());
        }

        private void AddReservedToken()
        {
            Debug.Assert(CharBuffer.Length != 0, "Can't add empty reserved token!");
            AddReservedToken(CharBuffer.ToString());
        }

        private void AddReservedToken(string s)
        {
            Debug.Assert(!UsedIdentifiers.ContainsKey(s));

            Token token;

            if (Tokens.ReservedMap.TryGetValue(s, out token!))
            {
                AddToken(token with { Line = CurrentLine });
                return;
            }

            TokenizerError("Cannot add unmapped token!");
        }

        private void AddUsedToken()
        {
            AddUsedToken(CharBuffer.ToString());
        }

        private void AddUsedToken(string s)
        {
            Token token;

            if (UsedIdentifiers.TryGetValue(s, out token!))
            {
                AddToken(token with { Line = CurrentLine });
                return;
            }

            TokenizerError("Cannot add used identifier!");
        }

        private void AddAnyReservedToken()
        {
            if (CharBuffer.Length == 0)
            {
                Debug.Assert(CurrentTokenState == TokenType.Undefined);
                return;
            }

            AddAnyReservedToken(CharBuffer.ToString());
        }

        private void AddAnyReservedToken(string s)
        {
            Token token;

            if (Tokens.ReservedMap.TryGetValue(s, out token!) ||
                UsedIdentifiers.TryGetValue(s, out token!))
            {
                AddToken(token with { Line = CurrentLine });
                return;
            }

            TokenizerError("Cannot add unreserved token!");
        }

        private void AddToken(Token token)
        {
            TokenList.Add(token);
            CharBuffer.Clear();
            CurrentTokenState = TokenType.Undefined;
        }

        private void BufferCurrentChar()
        {
            CharBuffer.Append(CurrentChar);
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

        private void TokenizerError(string error)
        {
            throw new TokenizerException(CurrentLine, error);
        }

        #endregion Methods
    }

    public class TokenizerException : TranspilerException
    {
        public TokenizerException(uint line, string message) : base($"Line {line}: {message}") {}
    }
}
