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
            Gamma = new EditableSetting<double>(
                displayName: "Gamma",
                dataObject.Gamma,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator);
            Motivity = new EditableSetting<double>(
                displayName: "Motivity",
                dataObject.Motivity,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator);
            SyncSpeed = new EditableSetting<double>(
                displayName: "Sync Speed",
                dataObject.SyncSpeed,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator);
            Smoothness = new EditableSetting<double>(
                displayName: "Smoothness",
                dataObject.Smoothness,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator);
        }
    }
}
