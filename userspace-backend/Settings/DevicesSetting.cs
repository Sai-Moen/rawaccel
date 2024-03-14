using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Data;

namespace userspace_backend.Settings
{
    public class DevicesSetting
    {
        public Device Device { get; set; }

        protected Device LoadedDevice { get; set; }
    }
}
