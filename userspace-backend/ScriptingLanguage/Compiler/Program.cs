using System;
using System.Collections.Generic;

namespace userspace_backend.ScriptingLanguage.Compiler;

/// <summary>
/// Represents a program consisting of executable Instructions.
/// </summary>
public record Program(byte[] ByteCode, Number[] Data)
{
    public CodeAddress Length => (CodeAddress)ByteCode.Length;
    public int Arity { get; set; } = 0;

    public byte this[CodeAddress index] => ByteCode[index.ToIndex()];
    public Number this[DataAddress index] => Data[index.ToIndex()];
}

/// <summary>
/// Replacement for Stack that actually allows for indexing.
/// </summary>
public class ProgramStack : List<Number>
{
    public Number this[StackAddress index]
    {
        get => this[index.ToIndex()];
        set => this[index.ToIndex()] = value;
    }

    public void Push(Number number)
    {
        Add(number);
    }

    public Number Pop()
    {
        int last = Count - 1;
        Number result = this[last];
        RemoveAt(last);
        return result;
    }
}
