using scripting;
using scripting.Generation;
using System.Diagnostics;

namespace scripting_tests.Old;

public static class Test
{
    public const string DebugPath = @"../../scripting/Spec/arc.ras";

    private static readonly Script script = new();

    public static void Log(string message)
    {
        Trace.WriteLine(message);
    }

    public static Interpreter CreateInterpreter()
    {
        script.LoadScript(DebugPath);
        return script.Interpreter;
    }
}
