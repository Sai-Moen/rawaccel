using System.Collections.Generic;
using System.IO;
using System.Linq;
using userspace_backend.Data;
using userspace_backend.IO;

namespace userspace_backend.Settings
{
    public class Settings
    {
        protected static DevicesReaderWriter DevicesReaderWriter = new DevicesReaderWriter();
        protected static MappingReaderWriter MappingReaderWriter = new MappingReaderWriter ();
        protected static ProfileReaderWriter ProfileReaderWriter = new ProfileReaderWriter ();

        public IList<Device> Devices { get; protected set; }

        public IList<Mapping> Mappings { get; protected set; }

        public IList<Profile> Profiles { get; protected set; }

        public void Load(string settingsDirectory)
        {
            LoadDevices(settingsDirectory);
            LoadMappings(settingsDirectory);
            LoadProfiles(settingsDirectory);
        }

        protected void LoadDevices(string settingsDirectory)
        {
            string devicesFilePath = GetDevicesFilePath(settingsDirectory);
            Devices = GetDevices(devicesFilePath);
        }

        protected IList<Device> GetDevices(string devicesFilePath)
        {
            string text = File.ReadAllText(devicesFilePath);
            return DevicesReaderWriter.Deserialize(text).ToList();
        }

        protected void LoadMappings(string settingsDirectory)
        {
            string mappingsDirectory = GetMappingsDirectory(settingsDirectory);
            IEnumerable<string> mappingsFiles = Directory.EnumerateFiles(mappingsDirectory).Where(f => f.EndsWith(".json"));
            Mappings = mappingsFiles.Select(f => GetMapping(f)).ToList();
        }

        protected Mapping GetMapping(string mappingFile)
        {
            string text = File.ReadAllText(mappingFile);
            return MappingReaderWriter.Deserialize(text);
        }

        protected void LoadProfiles(string settingsDirectory)
        {
            string profilesDirectory = GetProfilesDirectory(settingsDirectory);
            IEnumerable<string> profileFiles = Directory.EnumerateFiles(profilesDirectory).Where(f => f.EndsWith(".json"));
            Profiles = profileFiles.Select(f => GetProfile(f)).ToList();
        }

        protected Profile GetProfile(string mappingFile)
        {
            string text = File.ReadAllText(mappingFile);
            return ProfileReaderWriter.Deserialize(text);
        }

        protected static string GetDevicesFilePath(string settingsDirectory) => Path.Combine(settingsDirectory, "Devices.json");

        protected static string GetMappingsDirectory(string settingsDirectory) => Path.Combine(settingsDirectory, "Mappings");

        protected static string GetProfilesDirectory(string settingsDirectory) => Path.Combine(settingsDirectory, "Profiles");
    }
}
