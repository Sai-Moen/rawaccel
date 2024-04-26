using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Model;

namespace userinterface.ViewModels
{
    public class DeviceViewModel : ViewModelBase
    {
        public DeviceViewModel(DeviceModel deviceModel)
        {
            NameField = new EditableFieldViewModel("Name", deviceModel.Name);
            HWIDField = new EditableFieldViewModel("HWID", deviceModel.HardwareID);
            DPIField = new EditableFieldViewModel("DPI", deviceModel.DPI);
            PollRateField = new EditableFieldViewModel("PollRate", deviceModel.PollRate);
        }

        public EditableFieldViewModel NameField { get; set; }

        public EditableFieldViewModel HWIDField { get; set; }

        public EditableFieldViewModel DPIField { get; set; }

        public EditableFieldViewModel PollRateField { get; set; }
    }
}
