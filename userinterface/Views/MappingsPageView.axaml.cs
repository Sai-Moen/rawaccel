using Avalonia.Controls;
using Avalonia.Interactivity;
using userinterface.ViewModels;

namespace userinterface.Views;

public partial class MappingsPageView : UserControl
{
    public MappingsPageView()
    {
        InitializeComponent();
    }

    public void AddMapping(object sender, RoutedEventArgs args)
    {
        if (DataContext is MappingsPageViewModel viewModel)
        {
            _ = viewModel.TryAddNewMapping();
        }
    }
}