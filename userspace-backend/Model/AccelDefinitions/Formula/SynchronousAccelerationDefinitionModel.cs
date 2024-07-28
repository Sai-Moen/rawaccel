using System.Collections.Generic;
using System.Linq;
using userspace_backend.Data.Profiles;
using userspace_backend.Data.Profiles.Accel.Formula;
using userspace_backend.Model.EditableSettings;

namespace userspace_backend.Model.AccelDefinitions.Formula
{
    public class SynchronousAccelerationDefinitionModel : AccelDefinitionModel<SynchronousAccel>
    {
        public SynchronousAccelerationDefinitionModel(Acceleration dataObject) : base(dataObject)
        {
        }

        public EditableSetting<double> Gamma { get; set; }

        public EditableSetting<double> Motivity { get; set; }

        public EditableSetting<double> SyncSpeed { get; set; }

        public EditableSetting<double> Smoothness { get; set; }

        public override Acceleration MapToData()
        {
            return new SynchronousAccel()
            {
                Gamma = Gamma.ModelValue,
                Motivity = Motivity.ModelValue,
                SyncSpeed = SyncSpeed.ModelValue,
                Smoothness = Smoothness.ModelValue,
            };
        }

        protected override IEnumerable<IEditableSetting> EnumerateEditableSettings()
        {
            return [Gamma, Motivity, SyncSpeed, Smoothness];
        }

        protected override IEnumerable<IEditableSettingsCollection> EnumerateEditableSettingsCollections()
        {
            return Enumerable.Empty<IEditableSettingsCollection>();
        }

        protected override SynchronousAccel GenerateDefaultDataObject()
        {
            return new SynchronousAccel()
            {
                Gamma = 1,
                Motivity = 1.4,
                SyncSpeed = 12,
                Smoothness = 0.5,
            };
        }

        protected override void InitSpecificSettingsAndCollections(SynchronousAccel dataObject)
        {
            Gamma = new EditableSetting<double>(dataObject.Gamma, UserInputParsers.DoubleParser, ModelValueValidators.DefaultDoubleValidator);
            Motivity = new EditableSetting<double>(dataObject.Motivity, UserInputParsers.DoubleParser, ModelValueValidators.DefaultDoubleValidator);
            SyncSpeed = new EditableSetting<double>(dataObject.SyncSpeed, UserInputParsers.DoubleParser, ModelValueValidators.DefaultDoubleValidator);
            Smoothness = new EditableSetting<double>(dataObject.Smoothness, UserInputParsers.DoubleParser, ModelValueValidators.DefaultDoubleValidator);
        }
    }
}
