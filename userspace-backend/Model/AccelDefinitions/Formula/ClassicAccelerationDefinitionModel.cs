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
    public class ClassicAccelerationDefinitionModel : AccelDefinitionModel<ClassicAccel>
    {
        public ClassicAccelerationDefinitionModel(Acceleration dataObject) : base(dataObject)
        {
        }

        public EditableSetting<double> Acceleration { get; set; }

        public EditableSetting<double> Exponent { get; set; }

        public EditableSetting<double> Offset { get; set;  }

        public EditableSetting<double> Cap { get; set; }

        public override Acceleration MapToData()
        {
            return new ClassicAccel()
            {
                Acceleration = Acceleration.ModelValue,
                Exponent = Exponent.ModelValue,
                Offset = Offset.ModelValue,
                Cap = Cap.ModelValue,
            };

        }

        protected override IEnumerable<IEditableSetting> EnumerateEditableSettings()
        {
            return [ Acceleration, Exponent, Offset, Cap ];
        }

        protected override IEnumerable<IEditableSettingsCollection> EnumerateEditableSettingsCollections()
        {
            return Enumerable.Empty<IEditableSettingsCollection>();
        }

        protected override ClassicAccel GenerateDefaultDataObject()
        {
            return new ClassicAccel()
            {
                Acceleration = 0.001,
                Exponent = 2,
                Offset = 0,
                Cap = 0,
            };
        }

        protected override void InitSpecificSettingsAndCollections(ClassicAccel dataObject)
        {
            Acceleration = new EditableSetting<double>(dataObject.Acceleration, UserInputParsers.DoubleParser, ModelValueValidators.DefaultDoubleValidator);
            Exponent = new EditableSetting<double>(dataObject.Exponent, UserInputParsers.DoubleParser, ModelValueValidators.DefaultDoubleValidator);
            Offset = new EditableSetting<double>(dataObject.Offset, UserInputParsers.DoubleParser, ModelValueValidators.DefaultDoubleValidator);
            Cap = new EditableSetting<double>(dataObject.Cap, UserInputParsers.DoubleParser, ModelValueValidators.DefaultDoubleValidator);
        }
    }
}
