using Microsoft.VisualStudio.TestTools.UnitTesting;
using userspace_backend.ScriptingLanguage;

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
        IScriptFile scriptFile = Wrapper.LoadScript(Builtins.NO_ACCEL);
        Assert.AreEqual(NoAccel(), scriptFile.Calculate([0])[0]);
    }
}
