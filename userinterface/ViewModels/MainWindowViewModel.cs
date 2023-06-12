using System;
using System.Diagnostics;
using System.Text;
using userinterface.Models.Mouse;
using userinterface.Models.Script;

namespace userinterface.ViewModels
{
    public class MainWindowViewModel : ViewModelBase, IMouseMoveDisplayer
    {
        public MainWindowViewModel()
        {
#if DEBUG
            // Only for debugging RawAccelScript!
            Script script = new(Models.Script.Interaction.ScriptInterfaceType.Debug);
            script.LoadScript(Script.DebugPath);

            const int cap = 0x10000;
            double[] y = new double[cap];

            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < cap; i++)
            {
                y[i] = script.Interpreter.Calculate(i);
            }
            sw.Stop();

            script.UI.HandleMessage($"{sw.Elapsed.TotalMilliseconds} ms");

            Environment.Exit(0);
#else
            Profiles = new ProfilesViewModel();
            MouseListen = new MouseListenViewModel();
            MouseWindow = new MouseWindow(this);
#endif
        }

        public ProfilesViewModel Profiles { get; }

        public MouseListenViewModel MouseListen { get; }

        public MouseWindow MouseWindow { get; }

        public void SetLastMouseMove(float x, float y)
        {
            MouseListen.LastY = (int)y;
            MouseListen.LastX = (int)x;

            var size = MathF.Sqrt(x * x + y * y);
            Profiles.SetLastMouseMove(size, size);
        }
    }
}
