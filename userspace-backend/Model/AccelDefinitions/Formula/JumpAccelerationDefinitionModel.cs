using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Data.Profiles;
using userspace_backend.Data.Profiles.Accel.Formula;
using userspace_backend.Model.EditableSettings;

namespace userspace_backend.Model.AccelDefinitions.Formula
{
    public class JumpAccelerationDefinitionModel : AccelDefinitionModel<JumpAccel>
    {
        public JumpAccelerationDefinitionModel(Acceleration dataObject) : base(dataObject)
        {
        }

        public EditableSetting<double> Smooth { get; set; }

        public EditableSetting<double> Input { get; set; }

        public EditableSetting<double> Output { get; set; }

        public override Acceleration MapToData()
        {
            return new JumpAccel()
            {
                Smooth = Smooth.EditableValue,
                Input = Input.EditableValue,
                Output = Output.EditableValue
            };
        }

        protected override IEnumerable<IEditableSetting> EnumerateEditableSettings()
        {
            return [ Smooth, Input, Output ];
        }

        protected override IEnumerable<IEditableSettingsCollection> EnumerateEditableSettingsCollections()
        {
            return Enumerable.Empty<IEditableSettingsCollection>();
        }

        protected override JumpAccel GenerateDefaultDataObject()
        {
            return new JumpAccel()
            {
                Smooth = 0.5,
                Input = 15,
                Output = 1.5,
            };
        }

        protected override void InitSpecificSettingsAndCollections(JumpAccel dataObject)
        {
            Smooth = new EditableSetting<double>(dataObject.Smooth, UserInputParsers.DoubleParser);
            Input = new EditableSetting<double>(dataObject.Input, UserInputParsers.DoubleParser);
            Output = new EditableSetting<double>(dataObject.Output, UserInputParsers.DoubleParser);
        }
    }
}
