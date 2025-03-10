using Microsoft.VisualStudio.TestTools.UnitTesting;
using userspace_backend.ScriptingLanguage;

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

        IScriptFile scriptFile = Wrapper.LoadScript(script);
        Assert.AreEqual(3.0, scriptFile.Calculate([0])[0]);
    }
}
