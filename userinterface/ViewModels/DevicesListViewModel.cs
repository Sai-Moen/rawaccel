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
                ListViews.Add(new DeviceViewHolder(device, DevicesBE));
            }
        }

        private void DevicesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateListViews();
        }
    }

    public class DeviceViewHolder
    {
        private readonly BE.DeviceModel device;
        private readonly BE.DevicesModel devices;

        public DeviceViewHolder(BE.DeviceModel device, BE.DevicesModel devices)
        {
            this.devices = devices;
            this.device = device;
            DeviceView = new DeviceViewModel(device, devices.DeviceGroups);
        }

        public DeviceViewModel DeviceView { get; set; }

        public void DeleteSelf()
        {
            bool success = devices.RemoveDevice(device);
            Debug.Assert(success);
        }
    }
}
