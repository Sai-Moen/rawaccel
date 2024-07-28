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
            Devices = new DevicesModel();
            Mappings = new MappingSet();
            Profiles = new List<ProfileModel>();
        }

        public DevicesModel Devices { get; set; }

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
                Devices.TryAddDevice(deviceData);
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
            BackEndLoader.WriteSettingsToDisk(Devices.DevicesEnumerable);
        }

        protected void WriteToDriver()
        {
        }
    }
}
