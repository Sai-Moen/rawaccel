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

        public bool TryAddNewDeviceGroup()
        {
            for (int i = 1; i < 10; i++)
            {
                string newGroupName = $"DeviceGroup{i}";

                if (DeviceGroupsBE.TryAddDeviceGroup(newGroupName))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
