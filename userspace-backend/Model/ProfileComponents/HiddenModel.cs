using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Data.Profiles;
using userspace_backend.Model.EditableSettings;

namespace userspace_backend.Model.ProfileComponents
{
    public class HiddenModel : EditableSettingsCollection<Hidden>
    {
        public HiddenModel(Hidden dataObject) : base(dataObject)
        {
        }

        public EditableSetting<double> RotationDegrees { get; set; }

        public EditableSetting<double> AngleSnappingDegrees { get; set; }

        public EditableSetting<double> LeftRightRatio { get; set; }

        public EditableSetting<double> UpDownRatio { get; set; }

        public EditableSetting<double> SpeedCap { get; set; }

        public override Hidden MapToData()
        {
            return new Hidden()
            {
                RotationDegrees = RotationDegrees.ModelValue,
                AngleSnappingDegrees = AngleSnappingDegrees.ModelValue,
                LeftRightRatio = LeftRightRatio.ModelValue,
                UpDownRatio = UpDownRatio.ModelValue,
                SpeedCap = SpeedCap.ModelValue,
            };
        }

        protected override IEnumerable<IEditableSetting> EnumerateEditableSettings()
        {
            return [ RotationDegrees, AngleSnappingDegrees, LeftRightRatio, UpDownRatio, SpeedCap ];
        }

        protected override IEnumerable<IEditableSettingsCollection> EnumerateEditableSettingsCollections()
        {
            return Enumerable.Empty<IEditableSettingsCollection>();
        }

        protected override void InitEditableSettingsAndCollections(Hidden dataObject)
        {
            RotationDegrees = new EditableSetting<double>(
                displayName: "Rotation",
                initialValue: dataObject.RotationDegrees,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator);
            AngleSnappingDegrees = new EditableSetting<double>(
                displayName: "Angle Snapping",
                initialValue: dataObject.AngleSnappingDegrees,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator);
            LeftRightRatio = new EditableSetting<double>(
                displayName: "L/R Ratio",
                dataObject.LeftRightRatio,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator);
            UpDownRatio = new EditableSetting<double>(
                displayName: "U/D Ratio",
                dataObject.UpDownRatio,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator);
            SpeedCap = new EditableSetting<double>(
                displayName: "Speed Cap",
                dataObject.SpeedCap,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator);
        }
    }
}
