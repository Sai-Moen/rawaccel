﻿using scripting;
using scripting.Interpretation;
using System;

namespace scripting_tests.FormulaTests;

[TestClass]
public class PowerTest
{
    public const string POWER =
        """
        Legacy output cap Power mode as a RawAccelScript.

        [

            Scale    := 1    (0};
            Cap      := 0    [0};
            Exponent := 0.05 (0};
            Offset   := 0    [0};

        ]

            offset_x := (Offset / (Exponent + 1)) ^ (1 / Exponent) / Scale;
            constant := offset_x * Offset * Exponent / (Exponent + 1);

        {

            if (x <= offset_x) {
                y := Offset;
            }
            if (x > offset_x) {
                y := (Scale * x) ^ Exponent + constant / x;
            }

            if (Cap) {
                y := min(y, Cap);
            }

        }
        """;

    private readonly IInterpreter interpreter = Wrapper.LoadScript(POWER);

    private readonly double scale = 1;
    private readonly double cap = 0;
    private readonly double exponent = 0.05;
    private readonly double offset = 0;

    private readonly double offset_x;
    private readonly double constant;

    public PowerTest()
    {
        offset_x = Math.Pow(offset / (exponent + 1), 1 / exponent) / scale;
        constant = offset_x * offset * exponent / (exponent + 1);
    }

    private double Power(double x)
    {
        double y;

        if (x <= offset_x)
        {
            y = offset;
        }
        else
        {
            y = Math.Pow(scale * x, exponent) + constant / x;
        }

        if (cap > 0)
        {
            y = Math.Min(y, cap);
        }

        return y;
    }

    [TestMethod]
    public void TestImplementationsEqual()
    {
        const double n = 0x1000;

        interpreter.Init();
        for (int x = 1; x <= n; x++)
        {
            Assert.AreEqual(Power(x), interpreter.Calculate(x));
        }
    }
}