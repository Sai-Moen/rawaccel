using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Model;

namespace userinterface.ViewModels
{
    public class DevicesViewModel : ViewModelBase
    {
        public DevicesViewModel(IEnumerable<DeviceModel> deviceModels)
        {
            Devices = new ObservableCollection<DeviceViewModel>(deviceModels.Select(deviceModel => new DeviceViewModel(deviceModel)));
        }

        public ObservableCollection<DeviceViewModel> Devices { get; }
    }
}
