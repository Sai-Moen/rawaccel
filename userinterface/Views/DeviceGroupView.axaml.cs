using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using userinterface.ViewModels;
using BE = userspace_backend.Model;

namespace userinterface.Views;

public partial class DeviceGroupView : UserControl
{
    public DeviceGroupView()
    {
        InitializeComponent();
    }

    public void DeleteSelf(object sender, RoutedEventArgs args)
    {
        if (DataContext is DeviceGroupViewModel viewModel)
        {
            viewModel.DeleteSelf();
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
        if (sender is TextBox senderTextBox)
        {
            if (senderTextBox.DataContext is BE.DeviceGroupModel deviceGroup)
            {
                deviceGroup.TryUpdateFromInterface();
            }
        }
    }
}