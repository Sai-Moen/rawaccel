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
            NameField = new EditableFieldViewModel<string>("Name", deviceModel.Name);
            HWIDField = new EditableFieldViewModel<string>("HWID", deviceModel.HardwareID);
            DPIField = new EditableFieldViewModel<int>("DPI", deviceModel.DPI);
            PollRateField = new EditableFieldViewModel<int>("PollRate", deviceModel.PollRate);
        }

        public EditableFieldViewModel<string> NameField { get; set; }

        public EditableFieldViewModel<string> HWIDField { get; set; }

        public EditableFieldViewModel<int> DPIField { get; set; }

        public EditableFieldViewModel<int> PollRateField { get; set; }
    }
}
