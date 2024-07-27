using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Data;
using userspace_backend.IO;
using userspace_backend.Model;

namespace userspace_backend
{
    public class BackEnd
    {

        public BackEnd(IBackEndLoader backEndLoader)
        {
            BackEndLoader = backEndLoader;
            DeviceGroups = new DeviceGroups();
            Devices = new List<DeviceModel>();
            Mappings = new MappingSet();
            Profiles = new List<ProfileModel>();
        }

        public IList<DeviceModel> Devices { get; set; }

        public DeviceGroups DeviceGroups { get; set; }

        public MappingSet Mappings { get; set; }

        public IList<ProfileModel> Profiles { get; set; }

        protected IBackEndLoader BackEndLoader { get; set; }

        public void Load()
        {
            IEnumerable<Device> devicesData = BackEndLoader.LoadDevices(); ;
            LoadDevicesFromData(devicesData);
        }

        protected void LoadDevicesFromData(IEnumerable<Device> devicesData)
        {
            foreach(var deviceData in devicesData)
            {
                DeviceGroupModel deviceGroup = DeviceGroups.AddOrGetDeviceGroup(deviceData.DeviceGroup);
                DeviceModel deviceModel = new DeviceModel(deviceData, deviceGroup);
                Devices.Add(deviceModel);
            }
        }

        public void Apply()
        {
            try
            {
                WriteToDriver();
            }
            catch (Exception ex)
            {
                return;
            }

            WriteSettingsToDisk();
        }

        protected void WriteSettingsToDisk()
        {
            BackEndLoader.WriteSettingsToDisk(Devices);
        }

        protected void WriteToDriver()
        {
        }
    }
}
