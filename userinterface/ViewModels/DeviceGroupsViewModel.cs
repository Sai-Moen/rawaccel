using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels
{
    public partial class DeviceGroupsViewModel : ViewModelBase
    {
        public DeviceGroupsViewModel(BE.DeviceGroups deviceGroupsBE)
        {
            DeviceGroupsBE = deviceGroupsBE;
        }

        protected BE.DeviceGroups DeviceGroupsBE { get; }

        public ObservableCollection<BE.DeviceGroupModel> DeviceGroups { get => DeviceGroupsBE.DeviceGroupModels; }
    }
}
