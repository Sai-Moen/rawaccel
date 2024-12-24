using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using userspace_backend.ScriptingLanguage;
using userspace_backend.ScriptingLanguage.Interpreter;
using userspace_backend.ScriptingLanguage.Script;

namespace userspace_backend_tests.ScriptingLanguageTests.BuiltinsTests;

public class LegacyMotivityAccel
{
    private double accel;
    private double motivity;
    private double midpoint;
    private double constant;

    private void Init()
    {
        accel = Math.Exp(GrowthRate);
        motivity = 2 * Math.Log(Motivity);
        midpoint = Math.Log(Midpoint);
        constant = -motivity / 2;
    }

    public LegacyMotivityAccel(double growthRate = 1, double motivity = 1.5, double midpoint = 5)
    {
        GrowthRate = growthRate;
        Motivity = motivity;
        Midpoint = midpoint;

        Init();
    }

    public double GrowthRate { get; }
    public double Motivity { get; }
    public double Midpoint { get; }

    public double Call(double x)
    {
        double denom = Math.Exp(accel * (midpoint - Math.Log(x))) + 1;
        return Math.Exp(motivity / denom + constant);
    }
}

/// <summary>
/// Ported from accel-lookup.hpp, because gain motivity needs it.
/// </summary>
public record FpRepRange(int Start, int Stop, int Num)
{
    public void ForEach(Action<double> fn)
    {
        // for some reason e starts at 0 in the header file, this is better imo
        for (int e = Start; e < Stop; e++)
        {
            double expScale = Math.ScaleB(1, e) / Num;
            for (int i = 0; i < Num; i++)
            {
                fn((i + Num) * expScale);
            }
        }

        fn(Math.ScaleB(1, Stop));
    }

    public int Size()
    {
        return (Stop - Start) * Num + 1;
    }
}

public class GainMotivityAccel
{
    private const int capacity = Constants.LUT_RAW_DATA_CAPACITY;
    private readonly float[] data = new float[capacity];

    // the velocity field is always true in the original implementation.
    // in this case since the scripting language works based on sens rather than velocity,
    // the opposite was hardcoded

    private readonly LegacyMotivityAccel sig;

    private readonly FpRepRange range = new(-3, 9, 8);
    //private readonly double xStart;

    public GainMotivityAccel(double growthRate = 1, double motivity = 1.5, double midpoint = 5)
    {
        //xStart = Math.ScaleB(1, range.Start);

        double sum = 0;
        double a = 0;
        sig = new(growthRate, motivity, midpoint);
        double SigmoidSum(double b)
        {
            const int partitions = 2;

            double interval = (b - a) / partitions;
            for (int i = 1; i <= partitions; i++)
            {
                sum += sig.Call(a + i * interval) * interval;
            }
            a = b;
            return sum;
        }

        int i = 0;
        range.ForEach((x) => data[i++] = (float)(SigmoidSum(x) / x));
    }

    public double GrowthRate { get => sig.GrowthRate; }
    public double Motivity { get => sig.Motivity; }
    public double Midpoint { get => sig.Midpoint; }

    public double Call(double x)
    {
        int e = Math.Min(Math.ILogB(x), range.Stop - 1);
        if (e >= range.Start)
        {
            int idxIntLogPart = e - range.Start;
            double idxFracLinPart = Math.ScaleB(x, -e) - 1;
            double idxF = range.Num * (idxIntLogPart + idxFracLinPart);

            uint idx = (uint)Math.Min((int)idxF, range.Size() - 2);
            if (idx < capacity - 1)
            {
                return Lerp(data[idx], data[idx + 1], idxF - idx);
            }
        }

        return data[0];
    }

    public static double Lerp(double a, double b, double t)
    {
        double x = a + t * (b - a);
        if ((t > 1) == (a < b))
        {
            return Math.Max(x, b);
        }
        return Math.Min(x, b);
    }
}

[TestClass]
public class MotivityTests
{
    [TestMethod]
    public void TestLegacyImplementationsEqual()
    {
        const int cap = Constants.LUT_POINTS_CAPACITY;

        IInterpreter interpreter = Wrapper.LoadScript(Builtins.MOTIVITY);
        Parameters parameters = interpreter.Settings;
        Callbacks callbacks = interpreter.Callbacks;

        IEnumerable<LegacyMotivityAccel> mots =
            [
                new(),
                new(2, 2, 32),
                new(4, 4, 4),
            ];

        double[] xs = new double[cap];
        for (int i = 0; i < cap; i++)
            xs[i] = i + 1;

        foreach (LegacyMotivityAccel mot in mots)
        {
            parameters[0].Value = Constants.LEGACY;
            parameters[1].Value = mot.GrowthRate;
            parameters[2].Value = mot.Motivity;
            parameters[3].Value = mot.Midpoint;

            double[] actual = callbacks.Calculate(xs);
            for (int i = 0; i < cap; i++)
            {
                double expected = mot.Call(xs[i]);
                Assert.IsTrue(Math.Abs(actual[i] - expected) <= 1e-6);
            }
        }
    }

    // haven't yet figured out how to make gain motivity work in the scripting language
    // we can't do the same as the header/test implementations, because those already have a lookup table to use in operator()
    // presumably the header scales x because it's a log-log, hence x is logarithmic as well, but I will look up how to do it later
    [Ignore]
    [TestMethod]
    public void TestGainImplementationsEqual()
    {
        const int cap = Constants.LUT_POINTS_CAPACITY;

        IInterpreter interpreter = Wrapper.LoadScript(Builtins.MOTIVITY);
        Parameters parameters = interpreter.Settings;
        Callbacks callbacks = interpreter.Callbacks;

        IEnumerable<GainMotivityAccel> mots =
            [
                new(),
                new(2, 2, 32),
                new(4, 4, 4),
            ];

        double[] xs = new double[cap];
        for (int i = 0; i < cap; i++)
            xs[i] = i + 1;

        foreach (GainMotivityAccel mot in mots)
        {
            parameters[0].Value = Constants.GAIN;
            parameters[1].Value = mot.GrowthRate;
            parameters[2].Value = mot.Motivity;
            parameters[3].Value = mot.Midpoint;

            double[] actual = callbacks.Calculate(xs);
            for (int i = 0; i < cap; i++)
            {
                double expected = mot.Call(xs[i]);
                Assert.IsTrue(Math.Abs(actual[i] - expected) <= 1e-6);
            }
        }
    }
}
