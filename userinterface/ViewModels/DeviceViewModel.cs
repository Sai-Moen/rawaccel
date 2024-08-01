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
            NameField = new EditableFieldViewModel(DeviceBE.Name);
            HWIDField = new EditableFieldViewModel(DeviceBE.HardwareID);
            DPIField = new EditableFieldViewModel(DeviceBE.DPI);
            PollRateField = new EditableFieldViewModel(DeviceBE.PollRate);
            IgnoreBool = new EditableBoolViewModel(DeviceBE.Ignore);
        }

        protected BE.DeviceModel DeviceBE { get; }

        public EditableFieldViewModel NameField { get; set; }

        public EditableFieldViewModel HWIDField { get; set; }

        public EditableFieldViewModel DPIField { get; set; }

        public EditableFieldViewModel PollRateField { get; set; }

        public EditableBoolViewModel IgnoreBool { get; set; }
    }
}
