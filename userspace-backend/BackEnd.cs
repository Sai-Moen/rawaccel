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
        public static DevicesReaderWriter DevicesReaderWriter = new DevicesReaderWriter();

        public IList<DeviceModel> Devices { get; set; }

        public IList<Mapping> Mappings { get; set; }

        public IList<Profile> Profiles { get; set; }

        public void Load(string settingsDirectory)
        {
            LoadDevices(settingsDirectory);
        }

        protected void LoadDevices(string settingsDirectory)
        {
            string devicesFile = Path.Combine(settingsDirectory, "devices.json");
            string devicesText = File.ReadAllText(devicesFile);
            IEnumerable<Device> devicesData = DevicesReaderWriter.Read(devicesText);
            Devices = devicesData.Select(d => new DeviceModel(d)).ToList();
        }

        public void Apply(string settingsDirectory)
        {
            try
            {
                WriteToDriver();
            }
            catch (Exception ex)
            {
                return;
            }

            WriteSettingsToDisk(settingsDirectory);
        }

        protected void WriteToDriver()
        {
        }

        protected void WriteSettingsToDisk(string settingsDirectory)
        {
            WriteDevices(settingsDirectory);
        }

        protected void WriteDevices(string settingsDirectory)
        {
            IEnumerable<Device> devicesData = Devices.Select(d => d.MapToData());
            string devicesFileText = DevicesReaderWriter.Serialize(devicesData);
            string devicesFilePath = GetDevicesFile(settingsDirectory);
            File.WriteAllText(devicesFilePath, devicesFileText);
        }

        protected static string GetDevicesFile(string settingsDirectory) => Path.Combine(settingsDirectory, "devices.json");

        protected static string GetMappingsDirectory(string settingsDirectory) => Path.Combine(settingsDirectory, "mappings");

        protected static string GetProfilesDirectory(string settingsDirectory) => Path.Combine(settingsDirectory, "profiles");
    }
}
