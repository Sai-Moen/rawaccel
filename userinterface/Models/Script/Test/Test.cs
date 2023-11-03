using System.Diagnostics;

namespace userinterface.Models.Script.Test;

public static class Test
{
    public const string DebugPath = @"../../../Models/Script/Spec/arc.rascript";

    public static void Log(string message)
    {
        Trace.WriteLine(message);
    }
}
