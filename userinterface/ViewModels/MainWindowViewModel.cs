using System;
using userinterface.Models.Mouse;
using userinterface.Models.Script;

namespace userinterface.ViewModels
{
    public class MainWindowViewModel : ViewModelBase, IMouseMoveDisplayer
    {
        public MainWindowViewModel()
        {
#if DEBUG // Only for debugging RawAccelScript! Remove from other repos if applicable
            
            Script transpiler = new(Script.__DebugPath);
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
