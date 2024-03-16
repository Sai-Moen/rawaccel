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
        private string _name;

        public DeviceViewModel(string name, string hwID, int dpi, int pollRate)
        {
            Name = name;
            HWID = hwID;
            DPI = dpi;
            PollRate = pollRate;
        }

        public string Name
        { get => _name; set { this.RaiseAndSetIfChanged(ref _name, value); DebugFunction(); } }

        public string HWID { get; }

        public int DPI { get; }

        public int PollRate { get; }

        private void DebugFunction()
        {
            Console.WriteLine("Got here");
        }
    }
}
