namespace userinterface.Models.Script.Interaction
{
    public class ReleaseInterface : IScriptInterface
    {
        public void HandleException(ScriptException exception)
        {
            System.Console.WriteLine(exception);
        }

        public void HandleMessage(string message)
        {
            System.Console.WriteLine(message);
        }
    }
}
