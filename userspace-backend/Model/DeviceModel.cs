using System;
using System.Collections.Generic;
using System.Linq;
using userspace_backend.Data;
using userspace_backend.Model.EditableSettings;

namespace userspace_backend.Model
{
    public class DeviceModel : EditableSettingsCollection<Device>
    {
        public DeviceModel(Device device, DeviceGroupModel deviceGroup)
            : base(device)
        {
            DeviceGroup = deviceGroup;
        }

        public EditableSetting<string> Name { get; protected set; }

        public EditableSetting<string> HardwareID { get; protected set; }

        public EditableSetting<int> DPI { get; protected set; }

        public EditableSetting<int> PollRate { get; protected set; }

        public EditableSetting<bool> Ignore { get; protected set; }

        public DeviceGroupModel DeviceGroup { get; protected set; }

        protected Func<bool> NameSetConditional { get; set; }

        protected Func<bool> HardwareIDSetConditional { get; set; }

        protected override IEnumerable<IEditableSetting> EnumerateEditableSettings()
        {
            return [Name, HardwareID, DPI, PollRate, Ignore];
        }

        protected override IEnumerable<IEditableSettingsCollection> EnumerateEditableSettingsCollections()
        {
            return [DeviceGroup];
        }

        protected override void InitEditableSettingsAndCollections(Device device)
        {
            Name = new EditableSetting<string>(device.Name, UserInputParsers.StringParser, ModelValueValidators.DefaultStringValidator);
            HardwareID = new EditableSetting<string>(device.HWID, UserInputParsers.StringParser, ModelValueValidators.DefaultStringValidator);
            DPI = new EditableSetting<int>(device.DPI, UserInputParsers.IntParser, ModelValueValidators.DefaultIntValidator);
            PollRate = new EditableSetting<int>(device.PollingRate, UserInputParsers.IntParser, ModelValueValidators.DefaultIntValidator);
            Ignore = new EditableSetting<bool>(device.Ignore, UserInputParsers.BoolParser, ModelValueValidators.DefaultBoolValidator);
        }

        public override Device MapToData()
        {
            return new Device()
            {
                Name = this.Name.ModelValue,
                HWID = this.HardwareID.ModelValue,
                DPI = this.DPI.ModelValue,
                PollingRate = this.PollRate.ModelValue,
                Ignore = this.Ignore.ModelValue,
                DeviceGroup = DeviceGroup.MapToData(),
            };
        }
    }
}
