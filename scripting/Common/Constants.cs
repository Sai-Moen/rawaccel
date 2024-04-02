using scripting.Interpretation;
using System.Diagnostics;

namespace scripting.Common;

/// <summary>
/// Constants used in the scripting namespace.
/// </summary>
internal static class Constants
{
    internal const int MAX_IDENTIFIER_LEN = 0x20;
    internal const int MAX_NUMBER_LEN = 0x40;

    internal const int CAPACITY = MemoryAddress.CAPACITY;
    internal const int MAX_PARAMETERS = 8;
    internal const int MAX_VARIABLES = CAPACITY - MAX_PARAMETERS;

    static Constants()
    {
        Debug.Assert(CAPACITY == MAX_PARAMETERS + MAX_VARIABLES);
    }
}
