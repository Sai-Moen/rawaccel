using ReactiveUI;
namespace userinterface.ViewModels
{
    public class MouseListenViewModel : ViewModelBase
    {
        protected int _displayedLastX = 0;
        protected int _displayedLastY = 0;

        protected int _lastX = 0;
        protected int _lastY = 0;

        public MouseListenViewModel()
        {
            DisplayUpdated = false;
        }

        public int LastX
        {
            get => _lastX;
            set
            {
                if (value != _lastX)
                {
                    _lastX = value;
                    DisplayUpdated = false;
                }
            }
        }

        public int LastY
        {
            get => _lastY;
            set
            {
                if (value != _lastY)
                {
                    _lastY = value;
                    DisplayUpdated = false;
                }
            }
        }

        public bool DisplayUpdated { get; private set; }

        public int DisplayedLastX
        {
            get => _displayedLastX;
            set => this.RaiseAndSetIfChanged(ref _displayedLastX, value);
        }

        public int DisplayedLastY
        {
            get => _displayedLastY;
            set => this.RaiseAndSetIfChanged(ref _displayedLastY, value);
        }

        public void UpdateDisplay()
        {
            DisplayedLastX = LastX;
            DisplayedLastY = LastY;
            DisplayUpdated = true;
        }
    }
}
