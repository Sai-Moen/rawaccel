using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using userspace_backend.ScriptingLanguage;
using userspace_backend.ScriptingLanguage.Interpreter;
using userspace_backend.ScriptingLanguage.Script;

namespace userspace_backend_tests.ScriptingLanguageTests.SystemTests;

[TestClass]
public class ControlFlowTests
{
    [TestMethod]
    public void TestElse()
    {
        static double EmulateScript(double x)
        {
            double y = 1;
            if (x > 16)
            {
                y += x;
            }
            else
            {
                y *= x;
            }
            return y;
        }

        const string script =
            """
            Else statement test.

            []

            {

                if (x > 16) {
                    y += x;
                } else {
                    y *= x;
                }

            }
            """;

        IInterpreter interpreter = Wrapper.LoadScript(script);
        Callbacks callbacks = interpreter.Callbacks;
        for (int x = 1; x <= 32; x++)
        {
            Assert.AreEqual(EmulateScript(x), callbacks.Calculate(x));
        }
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    public void TestNestedIfs(int value)
    {
        double EmulateScript()
        {
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

            return y;
        }

        const string script =
            """
            Nested if statements test.

            [

                a := 1;

            ]

                const b := a + 2;
                const c := b * 2;
                const d := c ^ 2;

            {

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

            }
            """;

        IInterpreter interpreter = Wrapper.LoadScript(script);
        Callbacks callbacks = interpreter.Callbacks;
        Parameters parameters = interpreter.Settings;
        parameters[0].Value = value;

        Assert.AreEqual(EmulateScript(), callbacks.Calculate(0));
    }
}
