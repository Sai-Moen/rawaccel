using System;

namespace userinterface.Models.Script.Frontend
{
    public enum ScriptUI
    {
        CommandLine,
        Graphical,
    }

    public interface IScriptUI
    {
        public void HandleException(TranspilerException exception);

        public void HandleMessage(string message);
    }

    public static class ScriptUIFactory
    {
        public static IScriptUI GetScriptUI(ScriptUI ui) =>
            ui switch
            {
                ScriptUI.CommandLine => new CommandLineUI(),
                ScriptUI.Graphical => throw new NotImplementedException(),
                _ => throw new NotImplementedException(),
        };
    }
}
