namespace userinterface.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel()
    {
        DevicesPage = new DevicesPageViewModel();
    }

    public DevicesPageViewModel DevicesPage { get; }
}
