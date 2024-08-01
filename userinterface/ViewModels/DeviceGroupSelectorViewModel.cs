using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels
{
    public partial class DeviceGroupSelectorViewModel : ViewModelBase
    {
        public DeviceGroupSelectorViewModel(BE.EditableSettings.IEditableSetting deviceGroupSetting, BE.DeviceGroups deviceGroupsBE)
        {
            DeviceGroupSettingBE = deviceGroupSetting;
            DeviceGroupsBE = deviceGroupsBE;
            DeviceGroupEntries = new ObservableCollection<DeviceGroupSelectorEntry>();
            RefreshDeviceGroupEntries();
        }

        protected BE.EditableSettings.IEditableSetting DeviceGroupSettingBE { get; set; }

        protected BE.DeviceGroups DeviceGroupsBE { get; }

        protected ObservableCollection<DeviceGroupSelectorEntry> DeviceGroupEntries { get; }

        protected class DeviceGroupSelectorEntry
        {
            public string DeviceGroupName { get; set; }

            public bool IsValidDeviceGroup { get; set; }
        }

        public void RefreshDeviceGroupEntries()
        {
            DeviceGroupEntries.Clear();

            foreach (var deviceGroupBE in DeviceGroupsBE.DeviceGroupModels)
            {
                DeviceGroupEntries.Add(new DeviceGroupSelectorEntry
                {
                    DeviceGroupName = deviceGroupBE.Name.EditedValueForDiplay,
                    IsValidDeviceGroup = true,
                });

                if (!DeviceGroupEntries.Any(
                    entry => string.Equals(
                        entry.DeviceGroupName,
                        DeviceGroupSettingBE.EditedValueForDiplay,
                        StringComparison.InvariantCultureIgnoreCase)))
                {
                    DeviceGroupEntries.Add(new DeviceGroupSelectorEntry
                    {
                        DeviceGroupName = DeviceGroupSettingBE.EditedValueForDiplay,
                        IsValidDeviceGroup = false,
                    });
                }
            }
        }
    }
}
