using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Linq;
using userinterface.ViewModels;
using userspace_backend.Model;

namespace userinterface.Views;

public partial class MappingView : UserControl
{
    public MappingView()
    {
        InitializeComponent();
    }

    public void AddMappingSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0
            && this.DataContext is MappingViewModel viewModel)
        {
            this.DeviceGroupSelectorToAddMapping.ItemsSource = Enumerable.Empty<DeviceGroupModel>();
            viewModel.HandleAddMappingSelection(e);
            this.DeviceGroupSelectorToAddMapping.ItemsSource = viewModel.MappingBE.DeviceGroupsStillUnmapped;
        }
    }
}