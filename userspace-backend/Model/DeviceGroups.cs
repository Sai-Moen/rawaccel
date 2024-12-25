using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Model.EditableSettings;

namespace userspace_backend.Model
{
    public class DeviceGroups : EditableSettingsCollection<IEnumerable<string>>
    {
        public static readonly DeviceGroupModel DefaultDeviceGroup =
            new DeviceGroupModel("Default", ModelValueValidators.AllChangesInvalidStringValidator);

        public DeviceGroups(IEnumerable<string> devices)
            : base(devices)
        {
            GroupNameChangeValidator = new DeviceGroupValidator(this);
        }

        public ObservableCollection<DeviceGroupModel> DeviceGroupModels { get; set; }

        protected DeviceGroupValidator GroupNameChangeValidator { get; set; }

        public DeviceGroupModel AddOrGetDeviceGroup(string deviceGroupName)
        {
            if (!TryGetDeviceGroup(deviceGroupName, out DeviceGroupModel? deviceGroup))
            {
                deviceGroup = new DeviceGroupModel(deviceGroupName, GroupNameChangeValidator);
                DeviceGroupModels.Add(deviceGroup);
            }

            return deviceGroup;
        }

        public override IEnumerable<string> MapToData()
        {
            return DeviceGroupModels.Select(g => g.ModelValue);
        }

        public bool TryAddDeviceGroup(string deviceGroupName)
        {
            // Do not add group if one already exists
            if (TryGetDeviceGroup(deviceGroupName, out _))
            {
                return false;
            }

            DeviceGroupModel deviceGroup = new DeviceGroupModel(deviceGroupName, GroupNameChangeValidator);
            DeviceGroupModels.Add(deviceGroup);

            return true;
        }

        public bool TryGetDeviceGroup(string name, out DeviceGroupModel? deviceGroup)
        {
            deviceGroup = DeviceGroupModels.FirstOrDefault(
                g => string.Equals(g.ModelValue, name, StringComparison.InvariantCultureIgnoreCase));

            return deviceGroup != null;
        }

        protected override IEnumerable<IEditableSetting> EnumerateEditableSettings()
        {
            return DeviceGroupModels;
        }

        protected override IEnumerable<IEditableSettingsCollection> EnumerateEditableSettingsCollections()
        {
            return [];
        }

        protected override void InitEditableSettingsAndCollections(IEnumerable<string> dataObject)
        {
            // This initialization does not set up all device group models.
            // That is done in backend construction in order to point the devices to their groups.
            DeviceGroupModels = new ObservableCollection<DeviceGroupModel>() { DefaultDeviceGroup };
        }
    }

    public class DeviceGroupValidator(DeviceGroups deviceGroups) : IModelValueValidator<string>
    {
        protected DeviceGroups DeviceGroups { get; } = deviceGroups;

        public bool Validate(string value)
        {
            return !DeviceGroups.TryGetDeviceGroup(value, out _);
        }
    }
}
