using System.Collections.Generic;
using System.Linq;
using userspace_backend.Data;
using userspace_backend.Model.EditableSettings;

namespace userspace_backend.Model
{
    public class DeviceModel : EditableSettingsCollection<Device>
    {
        public DeviceModel(Device device) : base(device)
        { }

        public EditableSetting<string> Name { get; protected set; }

        public EditableSetting<string> HardwareID { get; protected set; }

        public EditableSetting<int> DPI { get; protected set; }

        public EditableSetting<int> PollRate { get; protected set; }

        protected override IEnumerable<IEditableSetting> EnumerateEditableSettings()
        {
            return [Name, HardwareID, DPI, PollRate];
        }

        protected override IEnumerable<IEditableSettingsCollection> EnumerateEditableSettingsCollections()
        {
            return Enumerable.Empty<IEditableSettingsCollection>();
        }

        protected override void InitEditableSettingsAndCollections(Device device)
        {
            Name = new EditableSetting<string>(device.Name, UserInputParsers.StringParser);
            HardwareID = new EditableSetting<string>(device.HWID, UserInputParsers.StringParser);
            DPI = new EditableSetting<int>(device.DPI, UserInputParsers.IntParser);
            PollRate = new EditableSetting<int>(device.PollingRate, UserInputParsers.IntParser);
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
