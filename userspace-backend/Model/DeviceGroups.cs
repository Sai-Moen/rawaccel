using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace userspace_backend.Model
{
    public class DeviceGroups
    {
        public static readonly DeviceGroupModel DefaultDeviceGroup = new DeviceGroupModel("Default");

        public DeviceGroups()
        {
            DeviceGroupModels = new ObservableCollection<DeviceGroupModel>() { DefaultDeviceGroup };
        }

        public ObservableCollection<DeviceGroupModel> DeviceGroupModels { get; }

        public DeviceGroupModel AddOrGetDeviceGroup(string deviceGroupName)
        {
            if (!TryGetDeviceGroup(deviceGroupName, out DeviceGroupModel deviceGroup))
            {
                deviceGroup = new DeviceGroupModel(deviceGroupName);
                DeviceGroupModels.Add(deviceGroup);
            }

            return deviceGroup;
        }

        public bool TryAddDeviceGroup(string deviceGroupName)
        {
            // Do not add group if one already exists
            if (TryGetDeviceGroup(deviceGroupName, out _))
            {
                return false;
            }

            DeviceGroupModel deviceGroup = new DeviceGroupModel(deviceGroupName);
            DeviceGroupModels.Add(deviceGroup);

            return true;
        }

        protected bool TryGetDeviceGroup(string name, out DeviceGroupModel deviceGroup)
        {
            deviceGroup = DeviceGroupModels.FirstOrDefault(
                g => string.Equals(g.Name.ModelValue, name, StringComparison.InvariantCultureIgnoreCase));

            return deviceGroup != null;
        }
    }
}
