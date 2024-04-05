using scripting;
using scripting.Interpretation;
using scripting.Script;
using System.Diagnostics;

namespace scripting_tests.Old;

public class Misc
{
    public static void Perf()
    {
        IInterpreter interpreter = Wrapper.LoadScript(Builtins.ARC);
        interpreter.Init();
        Callbacks callbacks = interpreter.Callbacks;

        const int cap = 0x1000;
        double[] ys = new double[cap];

        Stopwatch sw = Stopwatch.StartNew();
        for (int i = 0; i < cap; i++)
        {
            ys[i] = callbacks.Calculate(interpreter, i);
        }
        sw.Stop();

        Test.Log(sw.Elapsed.TotalMilliseconds.ToString());
        Test.Log((ys[16] * 16).ToString());
    }
}