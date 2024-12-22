using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using userspace_backend.ScriptingLanguage;
using userspace_backend.ScriptingLanguage.Interpreter;
using userspace_backend.ScriptingLanguage.Script;

namespace userspace_backend_tests.ScriptingLanguageTests.BuiltinsTests;

public class MotivityAccel
{
    public const double DEFAULT_GROWTH_RATE = 1;
    public const double DEFAULT_MOTIVITY = 1.5;
    public const double DEFAULT_MIDPOINT = 5;

    private double accel;
    private double motivity;
    private double midpoint;
    private double constant;

    public MotivityAccel()
        : this(DEFAULT_GROWTH_RATE, DEFAULT_MOTIVITY, DEFAULT_MIDPOINT)
    { }

    public MotivityAccel(double growthRate, double motivity, double midpoint)
    {
        GrowthRate = growthRate;
        Motivity = motivity;
        Midpoint = midpoint;

        Init();
    }

    private void Init()
    {
        accel = Math.Exp(GrowthRate);
        motivity = 2 * Math.Log(Motivity);
        midpoint = Math.Log(Midpoint);
        constant = -motivity / 2;
    }

    public double GrowthRate { get; }
    public double Motivity { get; }
    public double Midpoint { get; }

    public double Legacy(double x)
    {
        double denom = Math.Exp(accel * (midpoint - Math.Log(x))) + 1;
        return Math.Exp(motivity / denom + constant);
    }

    public double[] Legacy(double[] xs)
    {
        return xs.Select(Legacy).ToArray();
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

        IEnumerable<MotivityAccel> mots =
            [
                new(),
                new(2, 2, 32),
                new(4, 4, 4),
            ];

        double[] xs = new double[cap];
        for (int i = 0; i < cap; i++)
            xs[i] = i + 1;

        foreach (MotivityAccel mot in mots)
        {
            // parameter[0] is Gain
            parameters[1].Value = mot.GrowthRate;
            parameters[2].Value = mot.Motivity;
            parameters[3].Value = mot.Midpoint;

            double[] actual = callbacks.Calculate(xs);
            for (int i = 0; i < cap; i++)
            {
                double expected = mot.Legacy(xs[i]);
                Assert.IsTrue(Math.Abs(actual[i] - expected) <= 1e-6);
            }
        }
    }
}
