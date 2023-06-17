using userinterface.Models.Script;
using userinterface.Models.Script.Generation;

namespace userinterface.ViewModels
{
    public class ScriptViewModel : ViewModelBase
    {
        public const int LUT_MAX = 0x100;

        private readonly Script Script = new(Models.Script.Interaction.ScriptInterfaceType.Release);

        public ScriptViewModel(string filePath = Script.DebugPath)
        {
            Script.LoadScript(filePath);
        }

        public ParameterPairs GetDefaults()
        {
            return Script.Interpreter.Defaults;
        }

        public ParameterPairs GetParameters()
        {
            return Script.Interpreter.Settings;
        }

        public void SetParameters(ParameterPairs parameters)
        {
            Script.Interpreter.Settings = parameters;
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
