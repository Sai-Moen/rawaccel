using Microsoft.VisualStudio.TestTools.UnitTesting;
using userspace_backend.ScriptingLanguage;
using userspace_backend.ScriptingLanguage.Interpreting;
using userspace_backend.ScriptingLanguage.Script;

namespace userspace_backend_tests.ScriptingLanguageTests.SystemTests;

[TestClass]
public class DeclarationTests
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

                const b := a + 1;
                const c := b + 1;
                const d := c + 1;

            {

                y := a + b + c + d;

            }
            """;

        IInterpreter interpreter = Wrapper.LoadScript(script);
        Parameters parameters = interpreter.Settings;
        parameters[0].Value = value;

        // a + (a + 1) + (a + 1 + 1) + (a + 1 + 1 + 1) = 4a + 6
        double expected = value * 4 + 6;

        Callbacks callbacks = interpreter.Callbacks;
        double actual = callbacks.Calculate(0);

        Assert.AreEqual(expected, actual);
    }
}
