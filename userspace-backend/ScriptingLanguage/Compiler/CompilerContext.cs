using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using userspace_backend.ScriptingLanguage.Compiler.Tokenizer;

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

public class CompilerContext
{
    private readonly ImmutableArray<string> symbolSideTable;

    public CompilerContext(IList<string> sideTable)
    {
        symbolSideTable = [.. sideTable];
    }

    public CompilerContext(List<string> sideTable)
    {
        symbolSideTable = [.. sideTable];
    }

    public string GetSymbol(Token token)
    {
        SymbolIndex symbolIndex = token.SymbolIndex;
        Debug.Assert(symbolIndex != SymbolIndex.Invalid, "You probably called this with a token that has a compile-time known symbol.");

        int index = (int)symbolIndex;
        if (index < 0)
            throw new CompilationException($"Invalid SymbolIndex: {index}", token);

        int len = symbolSideTable.Length;
        if (index >= len)
            throw new CompilationException($"SymbolIndex out of bounds: {index} >= {len}", token);

        return symbolSideTable[index];
    }
}
