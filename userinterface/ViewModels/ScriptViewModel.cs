using userinterface.Models.Script;
using userinterface.Models.Script.Test;

namespace userinterface.ViewModels
{
    public class ScriptViewModel : ViewModelBase
    {
        private readonly Script Script = new();

        public ScriptViewModel(string filePath = Models.Script.Test.Test.DebugPath)
        {
            Script.LoadScript(filePath);
            Misc.Perf(Script.Interpreter);
        }
    }
}
