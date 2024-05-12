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
    public class PowerAccelerationDefinitionModel : AccelDefinitionModel<PowerAccel>
    {
        public PowerAccelerationDefinitionModel(Acceleration dataObject) : base(dataObject)
        {
        }

        public EditableSetting<double> Scale { get; set; }

        public EditableSetting<double> Exponent { get; set; }

        public EditableSetting<double> OutputOffset { get; set; }

        public EditableSetting<double> Cap { get; set; }

        public override Acceleration MapToData()
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<IEditableSetting> EnumerateEditableSettings()
        {
            return [ Scale, Exponent, OutputOffset, Cap ];
        }

        protected override IEnumerable<IEditableSettingsCollection> EnumerateEditableSettingsCollections()
        {
            return Enumerable.Empty<IEditableSettingsCollection>();
        }

        protected override PowerAccel GenerateDefaultDataObject()
        {
            return new PowerAccel()
            {
                Scale = 1,
                Exponent = 0.05,
                Cap = 0,
                OutputOffset = 0,
            };
        }

        protected override void InitSpecificSettingsAndCollections(PowerAccel dataObject)
        {
            Scale = new EditableSetting<double>(dataObject.Scale, UserInputParsers.DoubleParser);
            Exponent = new EditableSetting<double>(dataObject.Exponent, UserInputParsers.DoubleParser);
            OutputOffset = new EditableSetting<double>(dataObject.OutputOffset, UserInputParsers.DoubleParser);
            Cap = new EditableSetting<double>(dataObject.Cap, UserInputParsers.DoubleParser);
        }
    }
}
