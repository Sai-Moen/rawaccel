using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Data;

namespace userspace_backend.Model
{
    public class DevicesModel
    {
        public DevicesModel()
        {
            Devices = new List<DeviceModel>();
            DeviceGroups = new DeviceGroups();
        }

        public DeviceGroups DeviceGroups { get; set; }

        public IEnumerable<DeviceModel> DevicesEnumerable { get => Devices; }

        protected IList<DeviceModel> Devices { get; set; }

        public bool TryAddDevice(Device deviceData)
        {
            if (DoesDeviceAlreadyExist(deviceData.Name, deviceData.HWID))
            {
                return false;
            }

            DeviceGroupModel deviceGroup = DeviceGroups.AddOrGetDeviceGroup(deviceData.DeviceGroup);
            DeviceModel deviceModel = new DeviceModel(deviceData, deviceGroup);
            Devices.Add(deviceModel);

            return true;
        }

        public bool DoesDeviceAlreadyExist(string name, string hwid)
        {
            return Devices.Any(d =>
                string.Equals(d.Name.ModelValue, name, StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(d.HardwareID.ModelValue, hwid, StringComparison.InvariantCultureIgnoreCase));
        }

        public bool DoesDeviceNameAlreadyExist(string name)
        {
            return Devices.Any(d =>
                string.Equals(d.Name.ModelValue, name, StringComparison.InvariantCultureIgnoreCase));
        }

        public bool DoesDeviceHardwareIDAlreadyExist(string hwid)
        {
            return Devices.Any(d =>
                string.Equals(d.HardwareID.ModelValue, hwid, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
