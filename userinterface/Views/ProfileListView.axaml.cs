using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Linq;
using userinterface.ViewModels;
using BE = userspace_backend.Model;

namespace userinterface.Views;

public partial class ProfileListView : UserControl
{
    public ProfileListView()
    {
        InitializeComponent();
    }

    public void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (this.DataContext is ProfileListViewModel viewModel
            && e.AddedItems.Count > 0
            && e.AddedItems[0] is BE.ProfileModel selectedProfile)
        {
            viewModel.CurrentSelectedProfile = selectedProfile;
        }
    }
}