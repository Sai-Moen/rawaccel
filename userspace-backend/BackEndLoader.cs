using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DATA = userspace_backend.Data;
using userspace_backend.IO;
using userspace_backend.Model;

namespace userspace_backend
{
    public interface IBackEndLoader
    {
        public IEnumerable<DATA.Device> LoadDevices();

        public DATA.MappingSet LoadMappings();

        public IEnumerable<DATA.Profile> LoadProfiles();

        public void WriteSettingsToDisk(IEnumerable<DeviceModel> devices);
    }

    public class BackEndLoader : IBackEndLoader
    {
        public static DevicesReaderWriter DevicesReaderWriter = new DevicesReaderWriter();

        public BackEndLoader(string settingsDirectory)
        {
            SettingsDirectory = settingsDirectory;
        }

        public string SettingsDirectory { get; private set; }

        public IEnumerable<DATA.Device> LoadDevices()
        {
            string devicesFile = GetDevicesFile(SettingsDirectory);
            string devicesText = File.ReadAllText(devicesFile);
            IEnumerable<DATA.Device> devicesData = DevicesReaderWriter.Read(devicesText);
            return devicesData;
        }

        public DATA.MappingSet LoadMappings()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<DATA.Profile> LoadProfiles()
        {
            throw new NotImplementedException();
        }

        public void WriteSettingsToDisk(IEnumerable<DeviceModel> devices)
        {
            WriteDevices(devices);
        }

        protected void WriteDevices(IEnumerable<DeviceModel> devices)
        {
            IEnumerable<DATA.Device> devicesData = devices.Select(d => d.MapToData());
            string devicesFileText = DevicesReaderWriter.Serialize(devicesData);
            string devicesFilePath = GetDevicesFile(SettingsDirectory);
            File.WriteAllText(devicesFilePath, devicesFileText);
        }

        protected static string GetDevicesFile(string settingsDirectory) => Path.Combine(settingsDirectory, "devices.json");

        protected static string GetMappingsDirectory(string settingsDirectory) => Path.Combine(settingsDirectory, "mappings");

        protected static string GetProfilesDirectory(string settingsDirectory) => Path.Combine(settingsDirectory, "profiles");
    }
}
