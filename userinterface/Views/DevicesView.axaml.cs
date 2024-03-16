using Avalonia.Controls;
using Avalonia.Input;
using Windows.Media.Devices;

namespace userinterface.Views
{
    public partial class DevicesView : UserControl
    {
        public DevicesView()
        {
            InitializeComponent();
        }

        public void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
            }
        }
    }
}
