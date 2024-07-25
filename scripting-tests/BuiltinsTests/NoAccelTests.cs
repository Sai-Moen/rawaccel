using scripting;
using scripting.Interpreting;
using scripting.Script;

namespace scripting_tests.BuiltinsTests;

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
