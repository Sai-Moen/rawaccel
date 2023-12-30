using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace userinterface.ViewModels
{
    public class DevicesViewModel : ViewModelBase
    {
        public DevicesViewModel()
        {
            Devices = new ObservableCollection<DeviceViewModel>
            {
                new DeviceViewModel("mouse1", "hwID1", 12000, 2000),
                new DeviceViewModel("mouse2", "hwID2", 12000, 2000),
                new DeviceViewModel("mouse3", "hwID3", 1600, 500),
            };
        }

        public ObservableCollection<DeviceViewModel> Devices { get; }
    }
}
