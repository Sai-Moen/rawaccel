using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Data;
using userspace_backend.Model;

namespace userspace_backend
{
    // TODO: remove before release
    public static class Bootstrapper
    {
        public static BackEnd CreateBootstrappingBackend()
        {
            Device[] devices =
            {
                new Device() { Name = "Superlight 2", DPI = 32000, HWID = @"HID\VID_046D&PID_C54D&MI_00", PollingRate = 1000, DeviceGroup = "Logitech Mice"},
                new Device() { Name = "Outset AX", DPI = 1200, HWID = @"HID\VID_3057&PID_0001", PollingRate = 1000, DeviceGroup = "Testing"},
                new Device() { Name = "Razer Viper 8K", DPI = 1200, HWID = @"HID\VID_31E3&PID_1310", PollingRate = 1000, DeviceGroup = "Testing"},
            };

            BackEnd backEnd = new BackEnd();
            backEnd.Devices = new List<DeviceModel>(devices.Select(d => new DeviceModel(d)));
            return backEnd;
        }
    }
}
