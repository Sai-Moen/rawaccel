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
        protected DeviceGroupSelectorEntry selectedEntry;

        public DeviceGroupSelectorViewModel(BE.DeviceModel device, BE.DeviceGroups deviceGroupsBE)
        {
            Device = device;
            DeviceGroupsBE = deviceGroupsBE;
            DeviceGroupEntries = new ObservableCollection<DeviceGroupSelectorEntry>();
            RefreshDeviceGroupEntries();
        }

        protected BE.DeviceModel Device { get; set; }

        protected BE.DeviceGroups DeviceGroupsBE { get; }

        public ObservableCollection<DeviceGroupSelectorEntry> DeviceGroupEntries { get; }

        public DeviceGroupSelectorEntry SelectedEntry
        {
            get => selectedEntry;
            set
            {
                if (DeviceGroupsBE.TryGetDeviceGroup(value.DeviceGroupName, out BE.DeviceGroupModel deviceGroup))
                {
                    Device.DeviceGroup = deviceGroup;
                    selectedEntry = value;
                }
            }
        }

        public void RefreshDeviceGroupEntries()
        {
            DeviceGroupEntries.Clear();

            foreach (var deviceGroupBE in DeviceGroupsBE.DeviceGroupModels)
            {
                DeviceGroupEntries.Add(new DeviceGroupSelectorEntry
                {
                    DeviceGroupName = deviceGroupBE.Name.ModelValue,
                    IsValidDeviceGroup = true,
                });
            }

            if (!DeviceGroupsBE.DeviceGroupModels.Any(g => g.Equals(Device.DeviceGroup)))           
            {
                DeviceGroupEntries.Add(new DeviceGroupSelectorEntry
                {
                    DeviceGroupName = Device.DeviceGroup.Name.ModelValue,
                    IsValidDeviceGroup = false,
                });
            }

            selectedEntry = DeviceGroupEntries.FirstOrDefault(
                e => string.Equals(e.DeviceGroupName, Device.DeviceGroup.Name.ModelValue, StringComparison.InvariantCultureIgnoreCase));
        }
    }

    public class DeviceGroupSelectorEntry
    {
        public string DeviceGroupName { get; set; }

        public bool IsValidDeviceGroup { get; set; }
    }

}
