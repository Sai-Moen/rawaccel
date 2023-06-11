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

            const int end = 0x80;
            const int cap = 0x100;
            const int div = cap / end;
            double[] ys = new double[cap];

            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < cap; i++)
            {
                double x = ((double)i) / div;
                ys[i] = script.Interpreter.Calculate(x);
            }
            sw.Stop();

            StringBuilder sb = new();
            for (int i = 0; i < cap; i++)
            {
                double x = ((double)i) / div;
                sb.AppendLine((ys[i] * x).ToString());
            }
            sb.AppendLine(sw.Elapsed.TotalMilliseconds.ToString());
            script.UI.HandleMessage(sb.ToString());

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
