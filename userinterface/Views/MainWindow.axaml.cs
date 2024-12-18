using Avalonia.Controls;
using Avalonia.Interactivity;
using userinterface.ViewModels;

namespace userinterface.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public void ApplyButtonHandler(object sender, RoutedEventArgs args)
    {
        if (this.DataContext is MainWindowViewModel viewModel)
        {
            viewModel.ApplyButtonClicked();
        }
    }
}