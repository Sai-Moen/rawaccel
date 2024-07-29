using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace userinterface.ViewModels
{
    public partial class DevicesPageViewModel : ViewModelBase
    {
        public DevicesPageViewModel()
        {
            DevicesList = new DevicesListViewModel();
            DeviceGroups = new DeviceGroupsViewModel();
        }

        public DevicesListViewModel DevicesList { get; }

        public DeviceGroupsViewModel DeviceGroups { get; }
    }
}
