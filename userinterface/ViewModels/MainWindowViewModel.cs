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
            // Only for debugging RawAccelScript!
            Script script = new(Models.Script.Interaction.ScriptInterfaceType.Release);
            script.LoadScript(Script.DebugPath);

            const int cap = 0x100;
            double[] y = new double[cap];

            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < cap; i++)
            {
                y[i] = script.Interpreter.Calculate(i);
            }
            sw.Stop();

            StringBuilder sb = new(cap);
            for (int i = 0; i < cap; i++)
            {
                sb.AppendLine(y[i].ToString());
            }
            script.UI.HandleMessage(sb.ToString());
            script.UI.HandleMessage($"{sw.Elapsed.TotalMilliseconds} ms");

            Environment.Exit(0);
#if false
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
