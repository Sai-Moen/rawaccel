using System;
using System.Diagnostics;

namespace userspace_backend.ScriptingLanguage.Compiler;

public enum MemoryAddress : byte;
public enum DataAddress : ushort;
public enum StackAddress : int;
public enum CodeAddress : int;

public static class Addresses
{
    public static Index ToIndex(this MemoryAddress address) => (byte)address;
    public static Index ToIndex(this DataAddress address) => (ushort)address;
    public static Index ToIndex(this StackAddress address) => (int)address;
    public static Index ToIndex(this CodeAddress address) => (int)address;


    public static ReadOnlySpan<byte> ToBytes(this MemoryAddress address)
    {
        return new([(byte)address]);
    }

    public static ReadOnlySpan<byte> ToBytes(this DataAddress address)
    {
        return BitConverter.GetBytes((ushort)address);
    }

    public static ReadOnlySpan<byte> ToBytes(this StackAddress address)
    {
        return BitConverter.GetBytes((int)address);
    }

    public static ReadOnlySpan<byte> ToBytes(this CodeAddress address)
    {
        return BitConverter.GetBytes((int)address);
    }


    public static MemoryAddress MemoryAddressFromBytes(ReadOnlySpan<byte> bytes)
    {
        Debug.Assert(bytes.Length == sizeof(MemoryAddress));
        return (MemoryAddress)bytes[0];
    }

    public static DataAddress DataAddressFromBytes(ReadOnlySpan<byte> bytes)
    {
        Debug.Assert(bytes.Length == sizeof(DataAddress));
        return (DataAddress)BitConverter.ToUInt16(bytes);
    }

    public static StackAddress StackAddressFromBytes(ReadOnlySpan<byte> bytes)
    {
        Debug.Assert(bytes.Length == sizeof(StackAddress));
        return (StackAddress)BitConverter.ToInt32(bytes);
    }

    public static CodeAddress CodeAddressFromBytes(ReadOnlySpan<byte> bytes)
    {
        Debug.Assert(bytes.Length == sizeof(CodeAddress));
        return (CodeAddress)BitConverter.ToInt32(bytes);
    }
}
