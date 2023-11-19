using userinterface.Models.Mouse;

namespace userinterface.ViewModels
{
    public class MainWindowViewModel : ViewModelBase, IMouseMoveDisplayer
    {
        public MainWindowViewModel()
        {
            Charts = new ChartsViewModel();
            MouseListen = new MouseListenViewModel();
            MouseWindow = new MouseWindow(this);
        }

        public ChartsViewModel Charts { get; }

        public MouseListenViewModel MouseListen { get; }

        public MouseWindow MouseWindow { get; }

        public void SetLastMouseMove(float x, float y)
        {
            MouseListen.LastY = (int)y;
            MouseListen.LastX = (int)x;

            Charts.SetLastMouseMove(x, y);
        }
    }
}
