using System;
using userspace_backend.ScriptingLanguage.Script;

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
        int addressLength = ((InstructionType)this[c]).AddressLength();
        ReadOnlySpan<byte> address = new(ByteCode, c.Address + 1, addressLength);
        c += addressLength;
        return address;
    }
}