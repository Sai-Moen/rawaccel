using System;

namespace userinterface.ViewModels
{
    public class ViewSelectorViewModel : ViewModelBase
    {
        public ViewSelectorViewModel()
        {
            Devices = new DevicesViewModel();
            Profiles = new ProfilesViewModel();
        }

        public DevicesViewModel Devices { get; }

        public ProfilesViewModel Profiles { get; }
    }
}
