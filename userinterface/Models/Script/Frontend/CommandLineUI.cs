using System.Diagnostics;

namespace userinterface.Models.Script.Frontend
{
    public class CommandLineUI : IScriptUI
    {
        public void HandleException(TranspilerException exception)
        {
            string message = exception.Message;
#if DEBUG
            Debug.WriteLine(message);
#else
            Console.WriteLine(message);
#endif
        }

        public void HandleMessage(string message)
        {
#if DEBUG
            Debug.WriteLine(message);
#else
            Console.WriteLine(message);
#endif
        }
    }
}
