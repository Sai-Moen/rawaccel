using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace userinterface.Models.Script.Backend
{
    /**
     * <summary>
     * 
     * Phase of the Tokenizer, that determines the characters to accept.
     * 
     * <list type="table">
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
     * <description>Allows for up to <c>(32 - Parameters)</c> variables to be declared.</description>
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

    /// <summary>Automatically attempts to Tokenize when given an input script.</summary>
    internal class Tokenizer
    {
        #region Fields

        private TokenizerState State = TokenizerState.Startup;

        private TokenType CurrentTokenState = TokenType.Undefined;

        private const int MaxIdentifierLength = 0x10;

        private const int MaxLiteralLength = 0x20;

        private readonly StringBuilder TokenBuffer = new();

        internal readonly IList<Token> TokenList = new List<Token>();

        private readonly ISet<string> UsedIdentifiers = new HashSet<string>();

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
                .Replace(" ", "")
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

        private static bool IsAllowedCharacter(char c)
        {
            return IsAlphabeticCharacter(c) || IsNumericCharacter(c);
        }

        private bool IsReserved(string s)
        {
            return UsedIdentifiers.Contains(s) || Tokens.MapReserved.ContainsKey(s);
        }

        private void CheckCharacters()
        {
            foreach (char c in Characters)
            {
                if (IsAllowedCharacter(c) || IsReserved(c.ToString()))
                {
                    continue;
                }

                if (c == '\t')
                {
                    TokenizerError("Please use spaces instead of tabs.");
                }

                TokenizerError($"Invalid character detected, char: {c}, u16: {(ushort)c}");
            }
        }

        private void Tokenize()
        {
            while (++CurrentIdx <= MaxIdx)
            {
                CurrentChar = Characters[CurrentIdx];

                if (CurrentIdx == MaxIdx && !CurrentCharStrCmp(Tokens.Separators.CALC_END))
                {
                    TokenizerError("End Of File reached unexpectedly!");
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
            if (CurrentCharStrCmp(Tokens.Separators.PARAMS_START))
            {
                State = TokenizerState.Parameters;
            }
        }

        private void OnStateParameters()
        {
            if (CurrentCharStrCmp(Tokens.Separators.PARAMS_END))
            {
                AddTokenIfUnreserved();
                AddReservedCharacter();
                State = TokenizerState.Variables;
                return;
            }

            PreCalculationHelper(TokenType.Parameter);
        }

        private void OnStateVariables()
        {
            if (CurrentCharStrCmp(Tokens.Separators.CALC_START))
            {
                AddTokenIfUnreserved();
                AddReservedCharacter();
                State = TokenizerState.Calculation;
                return;
            }

            PreCalculationHelper(TokenType.Variable);
        }

        private void OnStateCalculation()
        {
            if (CurrentCharStrCmp(Tokens.Separators.CALC_END))
            {
                CurrentIdx = MaxIdx;
                return;
            }

            bool isAlphabetic = IsAlphabeticCharacter(CurrentChar);
            bool isNumeric = IsNumericCharacter(CurrentChar);
            bool isSeparator = Tokens.Separators.Set.Contains(CurrentChar);
            bool isOperator = Tokens.Operators.Set.Contains(CurrentChar);

            switch (CurrentTokenState)
            {
                case TokenType.Undefined:
                    TokenBuffer.Append(CurrentChar);

                    if (isAlphabetic)
                    {
                        CurrentTokenState = TokenType.Identifier;
                        return;
                    }
                    else if (isNumeric)
                    {
                        CurrentTokenState = TokenType.Literal;
                        return;
                    }
                    else if (isSeparator)
                    {
                        CurrentTokenState = TokenType.Separator;
                        AddToken();
                        return;
                    }
                    else if (isOperator)
                    {
                        CurrentTokenState = TokenType.Operator;
                        return;
                    }

                    goto default;
                case TokenType.Identifier:
                    // TODO

                    goto default;
                case TokenType.Operator:
                    // TODO

                    goto default;
                case TokenType.Literal:
                    // TODO

                    goto default;
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
                    TokenBuffer.Append(CurrentChar);

                    if (isAlphabetic)
                    {
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
                    Debug.Assert(TokenBuffer.Length <= MaxIdentifierLength);
                    if (TokenBuffer.Length == MaxIdentifierLength)
                    {
                        TokenizerError($"Identifier name too long! (>= {MaxIdentifierLength} characters)");
                    }

                    if (isAlphabetic || isNumeric)
                    {
                        TokenBuffer.Append(CurrentChar);
                        return;
                    }

                    if (CurrentCharStrCmp(Tokens.Operators.ASSIGN))
                    {
                        CurrentTokenState = type;
                        AddTokenIfUnreserved();
                        AddReservedCharacter();
                        return;
                    }

                    goto default;
                case TokenType.Literal:
                    Debug.Assert(TokenBuffer.Length <= MaxLiteralLength);
                    if (TokenBuffer.Length == MaxLiteralLength)
                    {
                        TokenizerError($"Number literal too long! (>= {MaxLiteralLength} characters)");
                    }

                    if (isAlphabetic)
                    {
                        TokenizerError("Unexpected letter in a number literal!");
                    }
                    else if (isNumeric)
                    {
                        TokenBuffer.Append(CurrentChar);
                        return;
                    }

                    if (CurrentCharStrCmp(Tokens.Separators.TERMINATOR))
                    {
                        AddTokenIfUnreserved();
                        AddReservedCharacter();
                        return;
                    }

                    goto default;
                default:
                    TokenizerError($"Indeterminate state during {State}!");
                    return;
            }
        }

        private void AddToken()
        {
            AddToken(TokenBuffer.ToString());
        }

        private void AddToken(string finalToken)
        {
            AddToken(CurrentTokenState, finalToken);
        }

        private void AddToken(TokenType TokenState, string finalToken)
        {
            TokenList.Add(new Token(TokenState, finalToken));
            TokenBuffer.Clear();
            CurrentTokenState = TokenType.Undefined;
        }
        
        private void AddTokenIfUnreserved()
        {
            string finalToken = TokenBuffer.ToString();

            if (finalToken.Length == 0)
            {
                return;
            }

            if (IsReserved(finalToken))
            {
                TokenizerError("Identifier reserved!");
            }

            AddToken(finalToken);

            UsedIdentifiers.Add(finalToken);
        }

        private void AddReservedCharacter()
        {
            Token token;

            if (Tokens.MapReserved.TryGetValue(CurrentChar.ToString(), out token!))
            {
                TokenList.Add(token);
                return;
            }

            TokenizerError("Cannot add reserved character!");
        }

        private char? PeekNext()
        {
            return CurrentIdx < MaxIdx ? Characters[CurrentIdx + 1] : null;
        }

        private bool CurrentCharStrCmp(string s)
        {
            return CurrentChar == s[0];
        }

        private void TokenizerError(string error)
        {
            throw new Exception($"Index: {CurrentIdx}; " + error);
        }

        #endregion Methods
    }
}
