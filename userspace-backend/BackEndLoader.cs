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
    public interface IBackEndLoader
    {
        public IEnumerable<Device> LoadDevices();

        public MappingSet LoadMappings();

        public IEnumerable<ProfileModel> LoadProfiles();

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

        public IEnumerable<Device> LoadDevices()
        {
            string devicesFile = GetDevicesFile(SettingsDirectory);
            string devicesText = File.ReadAllText(devicesFile);
            IEnumerable<Device> devicesData = DevicesReaderWriter.Read(devicesText);
            return devicesData;

        }

        public MappingSet LoadMappings()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ProfileModel> LoadProfiles()
        {
            throw new NotImplementedException();
        }

        public void WriteSettingsToDisk(IEnumerable<DeviceModel> devices)
        {
            WriteDevices(devices);
        }

        protected void WriteDevices(IEnumerable<DeviceModel> devices)
        {
            IEnumerable<Device> devicesData = devices.Select(d => d.MapToData());
            string devicesFileText = DevicesReaderWriter.Serialize(devicesData);
            string devicesFilePath = GetDevicesFile(SettingsDirectory);
            File.WriteAllText(devicesFilePath, devicesFileText);
        }

        protected static string GetDevicesFile(string settingsDirectory) => Path.Combine(settingsDirectory, "devices.json");

        protected static string GetMappingsDirectory(string settingsDirectory) => Path.Combine(settingsDirectory, "mappings");

        protected static string GetProfilesDirectory(string settingsDirectory) => Path.Combine(settingsDirectory, "profiles");
    }
}
