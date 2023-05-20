using System;
using RawAccel.Models.Mouse;

namespace RawAccel.ViewModels
{
    public class MainWindowViewModel : ViewModelBase, IMouseMoveDisplayer
    {
        public MainWindowViewModel()
        {
            MouseListen = new MouseListenViewModel();
            MouseWindow = new MouseWindow(this);
            Profiles = new ProfilesViewModel();
            Chart = new ChartViewModel();
        }

        public MouseListenViewModel MouseListen { get; }

        public MouseWindow MouseWindow { get; }

        public ProfilesViewModel Profiles { get; }

        public ChartViewModel Chart { get; }

        public void SetLastMouseMove(float x, float y)
        {
            MouseListen.LastY = (int)y;
            MouseListen.LastX = (int)x;

            var size = MathF.Sqrt(x * x + y * y);
            Chart.SetLastMouseMove(size, size);
        }
    }
}
