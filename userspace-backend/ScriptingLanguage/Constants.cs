using userspace_backend.ScriptingLanguage.Compiler.CodeGen;

namespace userspace_backend.ScriptingLanguage;

/// <summary>
/// Constants used in the scripting namespace.
/// </summary>
public static class Constants
{
    public const int LUT_POINTS_CAPACITY = 257;

    public const int MAX_IDENTIFIER_LEN = 0x20;
    public const int MAX_NUMBER_LEN = 0x40;

    public const int CAPACITY = MemoryAddress.CAPACITY;
    public const int MAX_PARAMETERS = 8;
    public const int MAX_DECLARATIONS = CAPACITY - MAX_PARAMETERS;

    public const int MAX_RECURSION_DEPTH = 0x1000;
}
