using scripting;
using scripting.Interpretation;

namespace scripting_tests.Old;

[TestClass]
public class Misc
{
    [TestMethod]
    public void Perf()
    {
        IInterpreter interpreter = Wrapper.LoadScript(Builtins.ARC);
        interpreter.Init();

        const int cap = 0x1000;
        double[] ys = new double[cap];

        Stopwatch sw = Stopwatch.StartNew();
        for (int i = 0; i < cap; i++)
        {
            ys[i] = interpreter.Calculate(i);
        }
        sw.Stop();

        Test.Log(sw.Elapsed.TotalMilliseconds.ToString());
        Test.Log((ys[16] * 16).ToString());
    }
}