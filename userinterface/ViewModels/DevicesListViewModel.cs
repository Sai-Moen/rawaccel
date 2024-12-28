using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels
{
    public partial class DevicesListViewModel : ViewModelBase
    {
        public DevicesListViewModel(BE.DevicesModel devicesBE)
        {
            DevicesBE = devicesBE;
            ListViews = new ObservableCollection<DeviceViewHolder>();
            UpdateListViews();
            DevicesBE.Devices.CollectionChanged += DevicesCollectionChanged;
        }

        protected BE.DevicesModel DevicesBE { get; set; }

        public ObservableCollection<BE.DeviceModel> Devices => DevicesBE.Devices;

        public ObservableCollection<DeviceViewHolder> ListViews { get; }

        public void UpdateListViews()
        {
            ListViews.Clear();

            foreach (BE.DeviceModel device in DevicesBE.Devices)
            {
                ListViews.Add(new DeviceViewHolder(this, device, DevicesBE.DeviceGroups));
            }
        }

        public void DeleteDevice(BE.DeviceModel device)
        {
            bool success = DevicesBE.Devices.Remove(device);
            Debug.Assert(success);
        }

        private void DevicesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateListViews();
        }
    }

    public class DeviceViewHolder
    {
        private readonly DevicesListViewModel parent;
        private readonly BE.DeviceModel device;

        public DeviceViewHolder(DevicesListViewModel parent, BE.DeviceModel device, BE.DeviceGroups groups)
        {
            this.parent = parent;
            this.device = device;
            DeviceView = new DeviceViewModel(device, groups);
        }

        public DeviceViewModel DeviceView { get; set; }

        public void DeleteSelf()
        {
            parent.DeleteDevice(device);
        }
    }
}
