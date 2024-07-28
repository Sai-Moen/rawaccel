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
    public class LinearAccelerationDefinitionModel : AccelDefinitionModel<LinearAccel>
    {
        public LinearAccelerationDefinitionModel(Acceleration dataObject) : base(dataObject)
        {
        }

        public EditableSetting<double> Acceleration { get; set; }

        public EditableSetting<double> Offset { get; set;  }

        public EditableSetting<double> Cap { get; set; }

        public override Acceleration MapToData()
        {
            return new LinearAccel()
            {
                Acceleration = Acceleration.ModelValue,
                Offset = Offset.ModelValue,
                Cap = Cap.ModelValue,
            };
        }

        protected override IEnumerable<IEditableSetting> EnumerateEditableSettings()
        {
            return [ Acceleration, Offset, Cap ];
        }

        protected override IEnumerable<IEditableSettingsCollection> EnumerateEditableSettingsCollections()
        {
            return Enumerable.Empty<IEditableSettingsCollection>();
        }

        protected override LinearAccel GenerateDefaultDataObject()
        {
            return new LinearAccel()
            {
                Acceleration = 0.001,
                Offset = 0,
                Cap = 0,
            };
        }

        protected override void InitSpecificSettingsAndCollections(LinearAccel dataObject)
        {
            Acceleration = new EditableSetting<double>(dataObject.Acceleration, UserInputParsers.DoubleParser, ModelValueValidators.DefaultDoubleValidator);
            Offset = new EditableSetting<double>(dataObject.Offset, UserInputParsers.DoubleParser, ModelValueValidators.DefaultDoubleValidator);
            Cap = new EditableSetting<double>(dataObject.Cap, UserInputParsers.DoubleParser, ModelValueValidators.DefaultDoubleValidator);
        }
    }
}
