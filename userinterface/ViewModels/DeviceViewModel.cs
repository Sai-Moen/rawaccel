using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels
{
    public partial class DeviceViewModel : ViewModelBase
    {
        public DeviceViewModel(BE.DeviceModel deviceBE, BE.DeviceGroups deviceGroupsBE)
        {
            DeviceBE = deviceBE;
            NameField = new NamedEditableFieldViewModel(DeviceBE.Name);
            HWIDField = new NamedEditableFieldViewModel(DeviceBE.HardwareID);
            DPIField = new NamedEditableFieldViewModel(DeviceBE.DPI);
            PollRateField = new NamedEditableFieldViewModel(DeviceBE.PollRate);
            IgnoreBool = new EditableBoolViewModel(DeviceBE.Ignore);
            DeviceGroup = new DeviceGroupSelectorViewModel(DeviceBE, deviceGroupsBE); ;
        }

        protected BE.DeviceModel DeviceBE { get; }

        public NamedEditableFieldViewModel NameField { get; set; }

        public NamedEditableFieldViewModel HWIDField { get; set; }

        public NamedEditableFieldViewModel DPIField { get; set; }

        public NamedEditableFieldViewModel PollRateField { get; set; }

        public EditableBoolViewModel IgnoreBool { get; set; }

        public DeviceGroupSelectorViewModel DeviceGroup { get; set; }
    }
}
