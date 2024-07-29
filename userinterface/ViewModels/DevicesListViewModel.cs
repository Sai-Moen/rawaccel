using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Model;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels
{
    public partial class DevicesListViewModel : ViewModelBase
    {
        public DevicesListViewModel(BE.DevicesModel devicesBE)
        {
            DevicesBE = devicesBE;
        }

        protected BE.DevicesModel DevicesBE { get; set; }

        public IEnumerable<BE.DeviceModel> Devices => DevicesBE.DevicesEnumerable;
    }
}
