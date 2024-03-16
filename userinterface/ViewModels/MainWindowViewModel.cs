using System;
using userinterface.Models.Mouse;
using userspace_backend;

namespace userinterface.ViewModels
{
    public class MainWindowViewModel : ViewModelBase, IMouseMoveDisplayer
    {
        public MainWindowViewModel()
        {
            BackEnd = new BackEnd();
            MouseWindow = new MouseWindow(this);
            ViewSelector = new ViewSelectorViewModel();
        }

        public ProfilesViewModel Profiles { get; }

        public MouseWindow MouseWindow { get; }

        public ViewSelectorViewModel ViewSelector { get; }

        public BackEnd BackEnd { get; }

        public string Test => "Is this working?";

        public void SetLastMouseMove(float x, float y)
        {
            ViewSelector.Profiles.MouseListen.LastY = (int)y;
            ViewSelector.Profiles.MouseListen.LastX = (int)x;
        }

        public void ShowLastMouseMove()
        {
            System.Diagnostics.Debug.WriteLine($"Updating last mouse move at time {DateTime.Now.Millisecond}");
            if (!ViewSelector.Profiles.MouseListen.DisplayUpdated)
            {
                ViewSelector.Profiles.MouseListen.UpdateDisplay();

                var size = MathF.Sqrt(ViewSelector.Profiles.MouseListen.LastX * ViewSelector.Profiles.MouseListen.LastX + ViewSelector.Profiles.MouseListen.LastY * ViewSelector.Profiles.MouseListen.LastY);
                Profiles.SetLastMouseMove(size, 1);
            }
        }
    }
}
