using scripting;
using scripting.Interpretation;
using scripting.Script;

namespace scripting_tests.InterpreterTests;

[TestClass]
public class InterpreterTest
{
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    public void TestVariableResolution(double value)
    {
        const string script =
            """
            [

                a := 1;

            ]

                b := a + 1;
                c := b + 1;
                d := c + 1;

            {

                y := a + b + c + d;

            }
            """;

        IInterpreter interpreter = Wrapper.LoadScript(script);

        Parameters parameters = interpreter.Settings;
        parameters[0].Value = value;

        interpreter.Init();
        Callbacks callbacks = interpreter.Callbacks;
        Assert.AreEqual(value * 4 + 6, callbacks.Calculate(interpreter, 0));
    }
}
