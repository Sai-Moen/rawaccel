using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Data;
using userspace_backend.Model;

namespace userspace_backend
{
    // TODO: remove before release
    public class Bootstrapper : IBackEndLoader
    {
        public Device[] DevicesToLoad { get; set; }

        public MappingSet MappingsToLoad { get; set; }

        public ProfileModel[] ProfilesToLoad { get; set; }

        public IEnumerable<Device> LoadDevices()
        {
            return DevicesToLoad;
        }

        public MappingSet LoadMappings()
        {
            return MappingsToLoad;
        }

        public IEnumerable<ProfileModel> LoadProfiles()
        {
            return ProfilesToLoad;
        }

        public void WriteSettingsToDisk(IEnumerable<DeviceModel> devices)
        {
            // Do nothing
        }
    }
}
