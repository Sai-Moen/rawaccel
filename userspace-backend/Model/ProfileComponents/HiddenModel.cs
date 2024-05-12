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
                RotationDegrees = RotationDegrees.EditableValue,
                AngleSnappingDegrees = AngleSnappingDegrees.EditableValue,
                LeftRightRatio = LeftRightRatio.EditableValue,
                UpDownRatio = UpDownRatio.EditableValue,
                SpeedCap = SpeedCap.EditableValue,
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
            RotationDegrees = new EditableSetting<double>(dataObject.RotationDegrees, UserInputParsers.DoubleParser);
            AngleSnappingDegrees = new EditableSetting<double>(dataObject.AngleSnappingDegrees, UserInputParsers.DoubleParser);
            LeftRightRatio = new EditableSetting<double>(dataObject.LeftRightRatio, UserInputParsers.DoubleParser);
            UpDownRatio = new EditableSetting<double>(dataObject.UpDownRatio, UserInputParsers.DoubleParser);
            SpeedCap = new EditableSetting<double>(dataObject.SpeedCap, UserInputParsers.DoubleParser);
        }
    }
}
