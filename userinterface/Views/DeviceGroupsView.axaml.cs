using Avalonia.Controls;
using Avalonia.Interactivity;
using userinterface.ViewModels;

namespace userinterface.Views;

public partial class DeviceGroupsView : UserControl
{
    public DeviceGroupsView()
    {
        InitializeComponent();
    }

    public void ClickHandler(object sender, RoutedEventArgs args)
    {
        if (this.DataContext is DeviceGroupsViewModel viewModel)
        {
            viewModel.TryAddNewDeviceGroup();
        }
    }
}