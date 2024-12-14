using System;
using System.Diagnostics;
using userspace_backend.ScriptingLanguage.Interpreting;
using userspace_backend.ScriptingLanguage.Script;

namespace userspace_backend.ScriptingLanguage.Generating;

/// <summary>
/// Represents an address in the Interpreter's Heap Memory.
/// </summary>
/// <param name="Address">Heap Memory address</param>
public readonly record struct MemoryAddress(byte Address)
{
    public const int SIZE = sizeof(byte);

    public const byte MAX_VALUE = byte.MaxValue;
    public const int CAPACITY = MAX_VALUE + 1;

    public static implicit operator MemoryAddress(byte pointer) => new(pointer);
    public static explicit operator MemoryAddress(ReadOnlySpan<byte> pointer) => pointer[0];

    public static implicit operator Index(MemoryAddress address) => address.Address;
    public static explicit operator byte[](MemoryAddress address) => [address.Address];
}

/// <summary>
/// Represents an address of static program data.
/// </summary>
/// <param name="Address">Data address</param>
public readonly record struct DataAddress(ushort Address)
{
    public const int SIZE = sizeof(ushort);

    public const ushort MAX_VALUE = ushort.MaxValue;
    public const int CAPACITY = MAX_VALUE + 1;

    public static implicit operator DataAddress(ushort pointer) => new(pointer);
    public static explicit operator DataAddress(ReadOnlySpan<byte> pointer) => BitConverter.ToUInt16(pointer);

    public static implicit operator Index(DataAddress address) => address.Address;
    public static explicit operator byte[](DataAddress address) => BitConverter.GetBytes(address.Address);
}

/// <summary>
/// Represents an Instruction address in the Program in which it is present.
/// </summary>
/// <param name="Address">Instruction address</param>
public readonly record struct CodeAddress(int Address)
{
    public const int SIZE = sizeof(int);

    public const int MAX_VALUE = int.MaxValue;
    public const long CAPACITY = MAX_VALUE + 1L;

    public static implicit operator CodeAddress(int pointer) => new(pointer);
    public static explicit operator CodeAddress(ReadOnlySpan<byte> pointer) => BitConverter.ToInt32(pointer);

    public static implicit operator Index(CodeAddress address) => address.Address;
    public static explicit operator byte[](CodeAddress address) => BitConverter.GetBytes(address.Address);

    public static bool operator <(CodeAddress left, CodeAddress right) => left.Address < right.Address;
    public static bool operator >(CodeAddress left, CodeAddress right) => left.Address > right.Address;

    public static CodeAddress operator ++(CodeAddress address) => address.Address + 1;

    public static CodeAddress operator +(CodeAddress left, CodeAddress right) => left.Address + right.Address;
}

/// <summary>
/// Represents a heap of memory used by:
/// Parameters (indices [0, 7])
/// Variables (indices after that, as many as needed)
/// </summary>
public class MemoryHeap
{
    private Number[] persistent = [];
    private Number[] impersistent = [];

    public Number GetPersistent(MemoryAddress address) => persistent[(Index)address];
    public void SetPersistent(MemoryAddress address, Number value) => persistent[(Index)address] = value;

    public Number GetImpersistent(MemoryAddress address) => impersistent[(Index)address];
    public void SetImpersistent(MemoryAddress address, Number value) => impersistent[(Index)address] = value;

    public void CopyFrom(MemoryHeap other)
    {
        int len = impersistent.Length;
        Debug.Assert(len == other.impersistent.Length);
        other.impersistent.CopyTo(impersistent, 0);
    }

    public void CopyAllFrom(MemoryHeap other)
    {
        CopyFrom(other);

        int len = persistent.Length;
        Debug.Assert(len == other.persistent.Length);
        other.persistent.CopyTo(persistent, 0);
    }

    public void EnsureSizes(int numPersistent, int numImpersistent)
    {
        persistent = new Number[numPersistent];
        impersistent = new Number[numImpersistent];
    }
}

/// <summary>
/// Represents the data cache, used for storing number literals from the code.
/// </summary>
public class StaticData
{
    private readonly Number[] Data;

    public StaticData(int capacity)
    {
        if (capacity > DataAddress.CAPACITY)
            throw new InterpreterException("StaticData capacity overflow!");

        Data = new Number[capacity];
    }

    public Number this[DataAddress address]
    {
        get => Data[address];
        set => Data[address] = value;
    }
}
