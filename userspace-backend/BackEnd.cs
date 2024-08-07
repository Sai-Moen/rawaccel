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
    public class BackEnd
    {

        public BackEnd(IBackEndLoader backEndLoader)
        {
            BackEndLoader = backEndLoader;
            Devices = new DevicesModel();
            Profiles = new ProfilesModel([]);
        }

        public DevicesModel Devices { get; set; }

        public MappingsModel Mappings { get; set; }

        public ProfilesModel Profiles { get; set; }

        protected IBackEndLoader BackEndLoader { get; set; }

        public void Load()
        {
            IEnumerable<DATA.Device> devicesData = BackEndLoader.LoadDevices(); ;
            LoadDevicesFromData(devicesData);

            IEnumerable<DATA.Profile> profilesData = BackEndLoader.LoadProfiles(); ;
            LoadProfilesFromData(profilesData);

            DATA.MappingSet mappingData = BackEndLoader.LoadMappings();
            Mappings = new MappingsModel(mappingData, Devices.DeviceGroups, Profiles);
        }

        protected void LoadDevicesFromData(IEnumerable<DATA.Device> devicesData)
        {
            foreach(var deviceData in devicesData)
            {
                Devices.TryAddDevice(deviceData);
            }
        }

        protected void LoadProfilesFromData(IEnumerable<DATA.Profile> profileData)
        {
            foreach (var profile in profileData)
            {
                Profiles.TryAddProfile(profile);
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
            MappingModel mappingToApply = Mappings.GetMappingToSetActive();
            DriverConfig config = MapToDriverConfig(mappingToApply);
            try
            {
                config.Activate();
            }
            catch(Exception ex)
            {
            }
        }

        protected DriverConfig MapToDriverConfig(MappingModel mappingModel)
        {
            IEnumerable<DeviceSettings> configDevices = MapToDriverDevices(mappingModel);
            IEnumerable<Profile> configProfiles = MapToDriverProfiles(mappingModel);

            DriverConfig config = DriverConfig.GetDefault();
            config.profiles = configProfiles.ToList();
            config.devices = configDevices.ToList();
            config.accels = configProfiles.Select(p => new ManagedAccel(p)).ToList();
            return config;
        }

        protected IEnumerable<DeviceSettings> MapToDriverDevices(MappingModel mapping)
        {
            IEnumerable<(DeviceGroupModel, string)> deviceGroupsToMap = mapping.IndividualMappings.AsReadOnly()
                .Select(m => { MappingGroup group = m.FirstOrDefault(); return (group.DeviceGroup, group.Profile.Name.ModelValue); });
            return deviceGroupsToMap.SelectMany(dg => MapToDriverDevices(dg.Item1, dg.Item2));
        }

        protected IEnumerable<Profile> MapToDriverProfiles(MappingModel mapping)
        {
            IEnumerable<ProfileModel> ProfilesToMap = mapping.IndividualMappings.AsReadOnly().Select(m => m.FirstOrDefault().Profile).Distinct();
            return ProfilesToMap.Select(MapToDriverProfile);
        }

        protected IEnumerable<DeviceSettings> MapToDriverDevices(DeviceGroupModel dg, string profileName)
        {
            IEnumerable<DeviceModel> deviceModels = Devices.Devices.Where(d => d.DeviceGroup.Equals(dg));
            return deviceModels.Select(dm => MapToDriverDevice(dm, profileName));
        }

        protected DeviceSettings MapToDriverDevice(DeviceModel deviceModel, string profileName)
        {
            return new DeviceSettings()
            {
                id = deviceModel.HardwareID.ModelValue,
                name = deviceModel.Name.ModelValue,
                profile = profileName,
                config = new DeviceConfig()
                {
                    disable = deviceModel.Ignore.ModelValue,
                    dpi = deviceModel.DPI.ModelValue,
                    pollingRate = deviceModel.DPI.ModelValue,
                    pollTimeLock = false,
                    setExtraInfo = false,
                    maximumTime = 200,
                    minimumTime = 0.1,
                }
            };
        }

        protected Profile MapToDriverProfile(ProfileModel profileModel)
        {
            return new Profile()
            {
                outputDPI = profileModel.OutputDPI.ModelValue,
                yxOutputDPIRatio = profileModel.YXRatio.ModelValue,
                argsX = profileModel.Acceleration.MapToDriver(),
                domainXY = new Vec2<double>
                {
                    x = profileModel.Anisotropy.DomainX.ModelValue,
                    y = profileModel.Anisotropy.DomainY.ModelValue,
                },
                rangeXY = new Vec2<double>
                {
                    x = profileModel.Anisotropy.RangeX.ModelValue,
                    y = profileModel.Anisotropy.RangeY.ModelValue,
                },
                rotation = profileModel.Hidden.RotationDegrees.ModelValue,
                lrOutputDPIRatio = profileModel.Hidden.LeftRightRatio.ModelValue,
                udOutputDPIRatio = profileModel.Hidden.UpDownRatio.ModelValue,
                snap = profileModel.Hidden.AngleSnappingDegrees.ModelValue,
                maximumSpeed = profileModel.Hidden.SpeedCap.ModelValue,
                minimumSpeed = 0,
            };
        }
    }
}
