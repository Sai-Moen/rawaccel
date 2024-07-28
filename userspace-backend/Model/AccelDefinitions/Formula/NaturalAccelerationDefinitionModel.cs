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
    public class NaturalAccelerationDefinitionModel : AccelDefinitionModel<NaturalAccel>
    {
        public NaturalAccelerationDefinitionModel(Acceleration dataObject) : base(dataObject)
        {
        }
        public EditableSetting<double> DecayRate { get; set; }

        public EditableSetting<double> InputOffset { get; set; }

        public EditableSetting<double> Limit { get; set; }

        public override Acceleration MapToData()
        {
            return new NaturalAccel()
            {
                DecayRate = DecayRate.ModelValue,
                InputOffset = InputOffset.ModelValue,
                Limit = Limit.ModelValue,
            };
        }

        protected override IEnumerable<IEditableSetting> EnumerateEditableSettings()
        {
            return [ DecayRate, InputOffset, Limit ];
        }

        protected override IEnumerable<IEditableSettingsCollection> EnumerateEditableSettingsCollections()
        {
            return Enumerable.Empty<IEditableSettingsCollection>();
        }

        protected override NaturalAccel GenerateDefaultDataObject()
        {
            return new NaturalAccel()
            {
                DecayRate = 0.1,
                InputOffset = 0,
                Limit = 1.5,
            };
        }

        protected override void InitSpecificSettingsAndCollections(NaturalAccel dataObject)
        {
            DecayRate = new EditableSetting<double>(dataObject.DecayRate, UserInputParsers.DoubleParser);
            InputOffset = new EditableSetting<double>(dataObject.InputOffset, UserInputParsers.DoubleParser);
            Limit = new EditableSetting<double>(dataObject.Limit, UserInputParsers.DoubleParser);
        }
    }
}
