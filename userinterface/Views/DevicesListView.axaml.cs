using Avalonia.Controls;
using Avalonia.Interactivity;
using userinterface.ViewModels;

namespace userinterface.Views;

public partial class DevicesListView : UserControl
{
    public DevicesListView()
    {
        InitializeComponent();
    }

    public void AddDevice(object sender, RoutedEventArgs args)
    {
        if (DataContext is DevicesListViewModel viewModel)
        {
            _ = viewModel.TryAddDevice();
        }
    }
}