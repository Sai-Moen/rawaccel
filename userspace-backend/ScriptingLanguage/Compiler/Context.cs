using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace userspace_backend.ScriptingLanguage.Compiler;

/// <summary>
/// Index of an identifier.
/// The lexer must maintain a side table with the symbols that the indices correspond to.
/// The reasoning for this is that punctuation and keywords don't need their textual representation to be stored (instead of potentially many times).
/// Making this an enum instead of just a uint increases type safety, e.g. no accidental implicit conversions w/ random integers.
/// </summary>
public enum SymbolIndex : int
{
    Invalid = -1
}

public readonly record struct FilePosition(int Line, int Column);

public class Context(string script)
{
    private readonly List<ReadOnlyMemory<char>> symbolSideTable = [];

    public string Script { get; } = script;

    public void Reset()
    {
        symbolSideTable.Clear();
    }

    public SymbolIndex AddSymbol(ReadOnlyMemory<char> charView)
    {
        SymbolIndex index = (SymbolIndex)symbolSideTable.Count;
        symbolSideTable.Add(charView);
        return index;
    }

    public SymbolIndex AddSymbol(int start, int length)
    {
        // AsMemory throws for most of these but additionally the lexer shouldn't be able to produce 0 length tokens
        Debug.Assert(length > 0, "Length must be positive (most likely a lexer bug).");

        return AddSymbol(Script.AsMemory(start, length));
    }

    public string GetSymbol(SymbolIndex symbolIndex)
    {
        bool ok = TryGetSymbol(symbolIndex, out string symbol);
        Debug.Assert(ok);
        return symbol;
    }

    public string GetSymbol(Token token)
    {
        return GetSymbol(token.SymbolIndex);
    }

    public bool TryGetSymbol(SymbolIndex symbolIndex, out string symbol)
    {
        Debug.Assert(symbolIndex != SymbolIndex.Invalid, "You probably called this with a token that has a compile-time known symbol.");

        int index = (int)symbolIndex;
        if (index < 0 || index >= symbolSideTable.Count)
        {
            // not sure if we want to do something better
            symbol = string.Empty;
            return false;
        }

        symbol = symbolSideTable[index].ToString();
        return true;
    }

    public bool TryGetSymbol(Token token, out string symbol)
    {
        return TryGetSymbol(token.SymbolIndex, out symbol);
    }

    public FilePosition FilePositionFromBytePosition(int bytePosition)
    {
        Debug.Assert(bytePosition < Script.Length);

        int line = 1;
        int column = 1;
        for (int i = 0; i < bytePosition; i++)
        {
            if (Script[i] == '\n')
            {
                ++line;
                column = 0;
            }
            ++column;
        }
        return new(line, column);
    }

    public FilePosition FilePositionFromToken(Token token)
    {
        return FilePositionFromBytePosition(token.BytePosition);
    }
}
