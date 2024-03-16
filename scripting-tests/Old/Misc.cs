using scripting.Generation;

namespace scripting_tests.Old;

[TestClass]
public class Misc
{
    [TestMethod]
    public static void Perf()
    {
        Interpreter interpreter = Test.CreateInterpreter();

        const int cap = 0x1000;
        double[] ys = new double[cap];

        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < cap; i++)
        {
            ys[i] = interpreter.Calculate(i);
        }
        sw.Stop();

        Test.Log(sw.Elapsed.TotalMilliseconds.ToString());
        Test.Log((ys[16] * 16).ToString());
    }
}