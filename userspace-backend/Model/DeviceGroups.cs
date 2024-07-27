using System;
using System.Collections.Generic;
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
            DeviceGroupModels = new HashSet<DeviceGroupModel>() { DefaultDeviceGroup };
        }

        protected HashSet<DeviceGroupModel> DeviceGroupModels { get; }

        public DeviceGroupModel AddOrGetDeviceGroup(string deviceGroupName)
        {
            DeviceGroupModel deviceGroup = DeviceGroupModels.FirstOrDefault(
                g => string.Equals(g.Name.EditableValue, deviceGroupName, StringComparison.InvariantCultureIgnoreCase));

            if (deviceGroup == null)
            {
                deviceGroup = new DeviceGroupModel(deviceGroupName);
                DeviceGroupModels.Add(deviceGroup);
            }

            return deviceGroup;
        }
    }
}
