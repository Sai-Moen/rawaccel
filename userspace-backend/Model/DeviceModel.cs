using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Data;

namespace userspace_backend.Model
{
    public class DeviceModel : EditableSettingsCollection<Device>
    {
        public DeviceModel(Device device) : base()
        { }

        public EditableSetting<string> Name { get; protected set; }

        public EditableSetting<string> HardwareID { get; protected set; }

        public EditableSetting<int> DPI { get; protected set; }

        public EditableSetting<int> PollRate { get; protected set; }

        protected override IEnumerable<IEditableSetting> GetEditableSettings()
        {
            return [Name, HardwareID, DPI, PollRate];
        }

        protected override IEnumerable<IEditableSettingsCollection> GetEditableSettingsCollections()
        {
            return Enumerable.Empty<IEditableSettingsCollection>();
        }

        protected override void InitEditableSettingsAndCollections(Device device)
        {
            Name = new EditableSetting<string>(device.Name);
            HardwareID = new EditableSetting<string>(device.HWID);
            DPI = new EditableSetting<int>(device.DPI);
            PollRate = new EditableSetting<int>(device.PollingRate);
        }

        public override Device MapToData()
        {
            return new Device()
            {
                Name = this.Name.EditableValue,
                HWID = this.HardwareID.EditableValue,
                DPI = this.DPI.EditableValue,
                PollingRate = this.PollRate.EditableValue,
            };
        }
    }
}
