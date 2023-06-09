using System.Diagnostics;

namespace userinterface.Models.Script.Interaction
{
    public class DebugInterface : IScriptInterface
    {
        public void HandleException(ScriptException exception)
        {
            Debug.WriteLine(exception);
        }

        public void HandleMessage(string message)
        {
            Debug.WriteLine(message);
        }
    }
}
