using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using userinterface.ViewModels;
using BE = userspace_backend.Model;

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

    public void TextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return)
        {
            TopLevel.GetTopLevel(this)?.FocusManager?.ClearFocus();
        }
    }

    public void LostFocusHandler(object sender, RoutedEventArgs args)
    {
        if(sender is TextBox senderTextBox)
        {
            if (senderTextBox.DataContext is BE.DeviceGroupModel deviceGroup)
            {
                deviceGroup.TryUpdateFromInterface();
            }
        }
    }
}