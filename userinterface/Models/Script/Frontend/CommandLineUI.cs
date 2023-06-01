using System;
using System.Diagnostics;

namespace userinterface.Models.Script.Frontend
{
    public class CommandLineUI : IScriptUI
    {
        public void HandleException(Exception exception)
        {
#if DEBUG
            Debug.WriteLine(exception.Message);
#else
            Console.WriteLine(exception.Message);
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
