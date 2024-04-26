using System;
using System.Collections.Generic;
using userspace_backend.Model;

namespace userinterface.ViewModels
{
    public class ViewSelectorViewModel : ViewModelBase
    {
        public ViewSelectorViewModel(IEnumerable<DeviceModel> deviceModels)
        {
            Devices = new DevicesViewModel(deviceModels);
            Profiles = new ProfilesViewModel();
        }

        public DevicesViewModel Devices { get; }

        public ProfilesViewModel Profiles { get; }
    }
}
