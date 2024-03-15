using scripting.Generation;

namespace scripting.Test;

public class Misc
{
    public static void Perf(Interpreter interpreter)
    {
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