using Microsoft.VisualStudio.TestTools.UnitTesting;
using userspace_backend.ScriptingLanguage;
using userspace_backend.ScriptingLanguage.Interpreter;
using userspace_backend.ScriptingLanguage.Script;

namespace userspace_backend_tests.ScriptingLanguageTests.SystemTests;

[TestClass]
public class FunctionTests
{
    [TestMethod]
    public void TestSimpleFunction()
    {
        const string script =
            """
            []

            fn testFunction(testLocal)
            {
                y += testLocal;
            }

            {
                y += testFunction(1);
            }
            """;

        IInterpreter interpreter = Wrapper.LoadScript(script);
        Callbacks callbacks = interpreter.Callbacks;

        Assert.AreEqual(3.0, callbacks.Calculate(0));
    }
}
