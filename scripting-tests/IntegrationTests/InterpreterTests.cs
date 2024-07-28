using scripting;
using scripting.Generating;
using scripting.Interpreting;
using scripting.Script;

namespace scripting_tests.IntegrationTests;

[TestClass]
public class InterpreterTests
{
    private static (Interpreter, Program[]) WastefulWayToGetInterpreterAndCallbackPrograms(string script)
    {
        // makes me twice as aware of compilation speed, by compiling separately 2 times for no good reason
        return (Wrapper.CompileToInterpreter(script), Wrapper.CompileToCallbackPrograms(script));
    }

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

                const b := a + 1;
                const c := b + 1;
                const d := c + 1;

            {

                y := a + b + c + d;

            }
            """;

        (Interpreter interpreter, Program[] programs) = WastefulWayToGetInterpreterAndCallbackPrograms(script);
        Parameters parameters = interpreter.Settings;
        parameters[0].Value = value;

        // a + (a + 1) + (a + 1 + 1) + (a + 1 + 1 + 1) = 4a + 6
        double expected = value * 4 + 6;

        interpreter.Init();
        Number[] remainder = interpreter.ExecuteProgram(programs[0]);
        Assert.AreEqual(0, remainder.Length);
        double actual = interpreter.Y;

        Assert.AreEqual(expected, actual);
    }
}
