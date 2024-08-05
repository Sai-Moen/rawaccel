using BE = userspace_backend;

namespace userinterface.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel(BE.BackEnd backEnd)
    {
        BackEnd = backEnd;
        DevicesPage = new DevicesPageViewModel(backEnd.Devices);
        ProfilesPage = new ProfilesPageViewModel(backEnd.Profiles);
    }

    public DevicesPageViewModel DevicesPage { get; }

    public ProfilesPageViewModel ProfilesPage { get; }

    protected BE.BackEnd BackEnd { get; set; }
}
