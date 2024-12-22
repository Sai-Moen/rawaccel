using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using userspace_backend.ScriptingLanguage;
using userspace_backend.ScriptingLanguage.Interpreter;
using userspace_backend.ScriptingLanguage.Script;

namespace userspace_backend_tests.ScriptingLanguageTests.BuiltinsTests;

public class ArcAccel
{
    public const double DEFAULT_INPUT_OFFSET = 0;
    public const double DEFAULT_LIMIT = 4;
    public const double DEFAULT_MIDPOINT = 16;

    private readonly double pLimit;

    public ArcAccel()
        : this(DEFAULT_INPUT_OFFSET, DEFAULT_LIMIT, DEFAULT_MIDPOINT)
    { }

    public ArcAccel(double inputOffset, double limit, double midpoint)
    {
        InputOffset = inputOffset;
        Limit = limit;
        Midpoint = midpoint;

        pLimit = Limit - 1;
    }

    public double InputOffset { get; }
    public double Limit { get; }
    public double Midpoint { get; }

    public double Arc(double x)
    {
        double y = 1;
        if (x > InputOffset)
        {
            x -= InputOffset;
            y += pLimit / x * (x - Midpoint * Math.Atan(x / Midpoint));
        }
        return y;
    }

    public double[] Arc(double[] xs)
    {
        return xs.Select(Arc).ToArray();
    }
}

[TestClass]
public class ArcTests
{
    [TestMethod]
    public void TestImplementationsEqual()
    {
        const int cap = Constants.LUT_POINTS_CAPACITY;

        IInterpreter interpreter = Wrapper.LoadScript(Builtins.ARC);
        Parameters parameters = interpreter.Settings;
        Callbacks callbacks = interpreter.Callbacks;

        IEnumerable<ArcAccel> arcs =
            [
                new(),
                new(2, 2, 32),
                new(4, 4, 4),
            ];

        double[] xs = new double[cap];
        for (int i = 0; i < cap; i++)
            xs[i] = i + 1;

        foreach (ArcAccel arc in arcs)
        {
            parameters[0].Value = arc.InputOffset;
            parameters[1].Value = arc.Limit;
            parameters[2].Value = arc.Midpoint;

            // somehow there are no floating point errors here
            // maybe look up if there is a method to include epsilon in the comparison or something
            // if this ever desyncs, change it to a for-loop with an epsilon
            CollectionAssert.AreEqual(arc.Arc(xs), callbacks.Calculate(xs));
        }
    }
}
