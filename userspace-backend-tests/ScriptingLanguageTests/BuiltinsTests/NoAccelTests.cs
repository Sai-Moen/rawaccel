using Microsoft.VisualStudio.TestTools.UnitTesting;
using userspace_backend.ScriptingLanguage;
using userspace_backend.ScriptingLanguage.Interpreter;
using userspace_backend.ScriptingLanguage.Script;

namespace userspace_backend_tests.ScriptingLanguageTests.BuiltinsTests;

// this test combats regression

[TestClass]
public class NoAccelTests
{
    private static double NoAccel()
    {
        return 1;
    }

    [TestMethod]
    public void TestImplementationsEqual()
    {
        IInterpreter interpreter = Wrapper.LoadScript(Builtins.NO_ACCEL);
        Callbacks callbacks = interpreter.Callbacks;

        Assert.AreEqual(NoAccel(), callbacks.Calculate(0));
    }
}
