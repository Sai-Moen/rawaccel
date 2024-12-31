using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels
{
    public partial class DeviceGroupsViewModel : ViewModelBase
    {
        public DeviceGroupsViewModel(BE.DeviceGroups deviceGroupsBE)
        {
            DeviceGroupsBE = deviceGroupsBE;
            ListViews = [];
            UpdateListViews();
            DeviceGroupsBE.DeviceGroupModels.CollectionChanged += DeviceGroupsCollectionChanged;
        }

        protected BE.DeviceGroups DeviceGroupsBE { get; }

        public ObservableCollection<BE.DeviceGroupModel> DeviceGroups => DeviceGroupsBE.DeviceGroupModels;
        public ObservableCollection<DeviceGroupViewHolder> ListViews { get; }

        public void UpdateListViews()
        {
            ListViews.Clear();
            foreach (BE.DeviceGroupModel deviceGroup in DeviceGroupsBE.DeviceGroupModels)
                ListViews.Add(new(deviceGroup, DeviceGroupsBE));
        }

        private void DeviceGroupsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateListViews();
        }

        public bool TryAddNewDeviceGroup()
        {
            for (int i = 1; i < 10; i++)
            {
                string newGroupName = $"DeviceGroup{i}";

                if (DeviceGroupsBE.TryAddDeviceGroup(newGroupName))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public class DeviceGroupViewHolder
    {
        private readonly BE.DeviceGroupModel deviceGroup;
        private readonly BE.DeviceGroups deviceGroups;

        public DeviceGroupViewHolder(BE.DeviceGroupModel deviceGroup, BE.DeviceGroups deviceGroups)
        {
            this.deviceGroup = deviceGroup;
            this.deviceGroups = deviceGroups;
            DeviceGroupView = new(deviceGroup);
        }

        public DeviceGroupViewModel DeviceGroupView { get; }

        public void DeleteSelf()
        {
            bool success = deviceGroups.RemoveDeviceGroup(deviceGroup);
            Debug.Assert(success);
        }
    }
}
