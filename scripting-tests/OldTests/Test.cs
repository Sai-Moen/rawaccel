using System.Diagnostics;

namespace scripting_tests.Old;

public static class Test
{
    public static void Main()
    {
        Misc.Perf();
    }

    public static void Log(string message)
    {
        Trace.WriteLine(message);
    }
}
