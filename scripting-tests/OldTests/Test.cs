namespace scripting_tests.Old;

public static class Test
{
    public static void Main()
    {
        new Misc().Perf();
    }

    public static void Log(string message)
    {
        Trace.WriteLine(message);
    }
}
