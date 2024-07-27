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

        protected HashSet<DeviceGroupModel> DeviceGroupModels { get; }
    }
}
