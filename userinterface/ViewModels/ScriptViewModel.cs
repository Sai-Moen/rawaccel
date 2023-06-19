using userinterface.Models.Script;

namespace userinterface.ViewModels
{
    public class ScriptViewModel : ViewModelBase
    {
        public const int LUT_MAX = 0x100; // Replace with global constant when available

        private readonly Script Script = new(Models.Script.Interaction.ScriptInterfaceType.Release);

        public ScriptViewModel(string filePath = Script.DebugPath)
        {
            Script.LoadScript(filePath);
        }

        public double[] GetRange(double end)
        {
            double step = end / LUT_MAX;

            double[] ys = new double[LUT_MAX];
            for (int i = 0; i < LUT_MAX; i++)
            {
                double x = step * i;
                ys[i] = Script.Interpreter.Calculate(x);
            }

            return ys;
        }
    }
}
