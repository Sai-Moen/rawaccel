using System.Diagnostics;

namespace userinterface.Models.Script.Interaction
{
    public class ReleaseInterface : IScriptInterface
    {
        public void HandleException(ScriptException exception)
        {
            Trace.WriteLine(exception);
        }

        public void HandleMessage(string message)
        {
            Trace.WriteLine(message);
        }
    }
}
