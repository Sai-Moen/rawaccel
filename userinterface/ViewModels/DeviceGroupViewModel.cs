using BE = userspace_backend.Model;

namespace userinterface.ViewModels
{
    public partial class DeviceGroupViewModel : ViewModelBase
    {
        public DeviceGroupViewModel(BE.DeviceGroupModel deviceGroup)
        {
            DeviceGroup = deviceGroup;
        }

        public BE.DeviceGroupModel DeviceGroup { get; }
    }
}
