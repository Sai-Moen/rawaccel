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
    public class MotivityAccelerationDefinitionModel : AccelDefinitionModel<MotivityAccel>
    {
        public MotivityAccelerationDefinitionModel(Acceleration dataObject) : base(dataObject)
        {
        }

        public EditableSetting<double> GrowthRate { get; set; }

        public EditableSetting<double> Motivity { get; set; }

        public EditableSetting<double> Midpoint { get; set; }

        public override Acceleration MapToData()
        {
            return new MotivityAccel()
            {
                GrowthRate = GrowthRate.EditableValue,
                Motivity = Motivity.EditableValue,
                Midpoint = Midpoint.EditableValue,
            };
        }

        protected override IEnumerable<IEditableSetting> EnumerateEditableSettings()
        {
            return [GrowthRate, Motivity, Midpoint];
        }

        protected override IEnumerable<IEditableSettingsCollection> EnumerateEditableSettingsCollections()
        {
            return Enumerable.Empty<IEditableSettingsCollection>();
        }

        protected override MotivityAccel GenerateDefaultDataObject()
        {
            return new MotivityAccel()
            {
                GrowthRate = 1,
                Motivity = 1.5,
                Midpoint = 5,
            };
        }

        protected override void InitSpecificSettingsAndCollections(MotivityAccel dataObject)
        {
            GrowthRate = new EditableSetting<double>(dataObject.GrowthRate, UserInputParsers.DoubleParser);
            Motivity = new EditableSetting<double>(dataObject.Motivity, UserInputParsers.DoubleParser);
            Midpoint = new EditableSetting<double>(dataObject.Midpoint, UserInputParsers.DoubleParser);
        }
    }
}
