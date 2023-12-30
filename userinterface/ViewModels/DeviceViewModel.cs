using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace userinterface.ViewModels
{
    public class DeviceViewModel
    {
        public DeviceViewModel(string name, string hwID, int dpi, int pollRate)
        {
            Name = name;
            HWID = hwID;
            DPI = dpi;
            PollRate = pollRate;
        }

        public string Name { get; }

        public string HWID { get; }

        public int DPI { get; }

        public int PollRate { get; }
    }
}
