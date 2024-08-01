using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
                ListViews.Add(new DeviceViewHolder(device, DevicesBE.DeviceGroups));
            }
        }

        private void DevicesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateListViews();
        }
    }

    public class DeviceViewHolder
    {
        public DeviceViewHolder(BE.DeviceModel device, BE.DeviceGroups groups)
        {
            DeviceView = new DeviceViewModel(device, groups);
        }

        public DeviceViewModel DeviceView { get; set; }
    }
}
