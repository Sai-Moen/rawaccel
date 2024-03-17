using scripting.Interpretation;

namespace scripting.Common;

/// <summary>
/// Constants used in the scripting namespace.
/// </summary>
public static class Constants
{
    public const int MAX_IDENTIFIER_LEN = 0x10;
    public const int MAX_NUMBER_LEN = 0x20;

    public const int CAPACITY = MemoryAddress.CAPACITY;
    public const int MAX_PARAMETERS = 8;
    public const int MAX_VARIABLES = CAPACITY - MAX_PARAMETERS;

    static Constants()
    {
        Debug.Assert(CAPACITY == MAX_PARAMETERS + MAX_VARIABLES);
    }
}
