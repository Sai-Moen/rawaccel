﻿using scripting;
using scripting.Interpreting;
using scripting.Script;
using System;

namespace scripting_tests.FormulaTests;

[TestClass]
public class LinearTest
{
    public const string LINEAR =
        """
        Legacy output cap Linear mode as a RawAccelScript.

        [

            Acceleration := 0.005 (0};
            Output_Cap   := 2     [0};
            Input_Offset := 0     [0};

        ]

            base := zero;

        {

            if (x <= Input_Offset) { ret; }

            base := Acceleration * (x - Input_Offset) ^ 2 / x;
            y += min(base, Output_Cap);

        }
        """;

    private readonly IInterpreter interpreter = Wrapper.LoadScript(LINEAR);

    private readonly double acceleration = 0.005;
    private readonly double cap = 2;
    private readonly double offset = 0;
    private readonly double exponent = 2;

    private readonly double raised;

    public LinearTest()
    {
        raised = Math.Pow(acceleration, exponent - 1);
    }

    private double Linear(double x)
    {
        if (x <= offset) return 1;

        double basenum = Math.Pow(x - offset, exponent) * raised / x;
        return Math.Min(basenum, cap) + 1;
    }

    [TestMethod]
    public void TestImplementationsEqual()
    {
        const double n = 0x1000;

        interpreter.Init();
        Callbacks callbacks = interpreter.Callbacks;
        for (int x = 1; x <= n; x++)
        {
            Assert.AreEqual(Linear(x), callbacks.Calculate(x));
        }
    }
}
