using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using BE = userspace_backend.Model;
using DATA = userspace_backend.Data;

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

        private void DevicesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateListViews();
        }

        public void UpdateListViews()
        {
            ListViews.Clear();
            foreach (BE.DeviceModel device in DevicesBE.Devices)
            {
                ListViews.Add(new DeviceViewHolder(device, DevicesBE));
            }
        }

        public bool TryAddDevice()
        {
            for (int i = 0; i < 10; i++)
            {
                DATA.Device device = new()
                {
                    Name = $"Device{i}",
                    HWID = $"{i}",
                    DPI = 1600,
                    PollingRate = 1000,
                    DeviceGroup = "Default",
                };

                if (DevicesBE.TryAddDevice(device))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public class DeviceViewHolder
    {
        private readonly BE.DeviceModel device;
        private readonly BE.DevicesModel devices;

        public DeviceViewHolder(BE.DeviceModel device, BE.DevicesModel devices)
        {
            this.device = device;
            this.devices = devices;
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
