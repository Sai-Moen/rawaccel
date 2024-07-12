using scripting.Generating;
using System.Diagnostics;

namespace scripting.Common;

/// <summary>
/// Constants used in the scripting namespace.
/// </summary>
public static class Constants
{
    public const int LUT_POINTS_CAPACITY = 256;

    public const int MAX_IDENTIFIER_LEN = 0x20;
    public const int MAX_NUMBER_LEN = 0x40;

    public const int CAPACITY = MemoryAddress.CAPACITY;
    public const int MAX_PARAMETERS = 8;
    public const int MAX_VARIABLES = CAPACITY - MAX_PARAMETERS;

    public const int MAX_RECURSION_DEPTH = 0x2000;

    static Constants()
    {
        Debug.Assert(CAPACITY == MAX_PARAMETERS + MAX_VARIABLES);
    }
}
