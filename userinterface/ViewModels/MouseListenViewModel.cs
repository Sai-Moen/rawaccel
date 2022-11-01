using ReactiveUI;
namespace userinterface.ViewModels
{
    public class MouseListenViewModel : ViewModelBase
    {
        protected int _lastX = 0;
        protected int _lastY = 0;

        public MouseListenViewModel()
        {
        }

        public int LastX
        {
            get => _lastX;
            set => this.RaiseAndSetIfChanged(ref _lastX, value);
        }

        public int LastY
        {
            get => _lastY;
            set => this.RaiseAndSetIfChanged(ref _lastY, value);
        }
    }
}
