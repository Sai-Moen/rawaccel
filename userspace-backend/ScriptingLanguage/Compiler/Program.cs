using System;
using System.Collections.Generic;

namespace userspace_backend.ScriptingLanguage.Compiler;

/// <summary>
/// Represents a program consisting of executable Instructions.
/// </summary>
public record Program(byte[] ByteCode, StaticData Data)
{
    public int Length => ByteCode.Length;
    public int Arity { get; set; } = 0;

    public byte this[CodeAddress index] => ByteCode[index];
    public Number this[DataAddress index] => Data[index];

    public ReadOnlySpan<byte> ExtractAddress(ref CodeAddress c)
    {
        int addressLength = ((InstructionKind)this[c]).AddressLength();
        ReadOnlySpan<byte> address = new(ByteCode, c.Address + 1, addressLength);
        c += addressLength;
        return address;
    }
}

/// <summary>
/// Replacement for Stack that actually allows for indexing.
/// </summary>
public class ProgramStack : List<Number>
{
    public Number this[StackAddress index]
    {
        get => this[(Index)index];
        set => this[(Index)index] = value;
    }

    public void Push(Number number)
    {
        Add(number);
    }

    public Number Pop()
    {
        StackAddress last = Count - 1;
        Number result = this[last];
        RemoveAt(last.Address);
        return result;
    }
}
