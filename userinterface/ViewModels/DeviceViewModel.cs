using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace userinterface.ViewModels
{
    public class DeviceViewModel : ViewModelBase
    {
        public DeviceViewModel(string name, string hwID, int dpi, int pollRate)
        {
            NameField = new EditableFieldViewModel("Name", name);
            HWIDField = new EditableFieldViewModel("HWID", hwID);
            DPIField = new EditableFieldViewModel("DPI", dpi.ToString());
            PollRateField = new EditableFieldViewModel("PollRate", pollRate.ToString());
        }

        public EditableFieldViewModel NameField { get; set; }

        public EditableFieldViewModel HWIDField { get; set; }

        public EditableFieldViewModel DPIField { get; set; }

        public EditableFieldViewModel PollRateField { get; set; }
    }
}
