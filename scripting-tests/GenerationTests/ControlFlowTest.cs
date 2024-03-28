using scripting;
using scripting.Interpretation;
using scripting.Script;
using System;

namespace scripting_tests.GenerationTests;

[TestClass]
public class ControlFlowTest
{
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    public void TestNestedIfs(int value)
    {
        const string script =
            """
            Nested if statements test.

            [
                a := 1;
            ]

                b := a + 2;
                c := b * 2;
                d := c ^ 2;

            {
                if (a > 1):
                    y += 1;
                    if (b > 4):
                        y += 2;
                        if (c > 8):
                            y += 4;
                            if (d > 64):
                                y += 8;
                            :
                        :
                    :
                :
            }
            """;

        double a = value;
        double b = a + 2;
        double c = b * 2;
        double d = Math.Pow(c, 2);

        double y = 1;

        // yandev approves
        if (a > 1)
        {
            y += 1;
            if (b > 4)
            {
                y += 2;
                if (c > 8)
                {
                    y += 4;
                    if (d > 64)
                    {
                        y += 8;
                    }
                }
            }
        }

        IInterpreter interpreter = Wrapper.LoadScript(script);
        Parameters parameters = interpreter.Settings;
        parameters[0].Value = value;

        interpreter.Init();
        Assert.AreEqual(y, interpreter.Calculate(0));
    }
}
