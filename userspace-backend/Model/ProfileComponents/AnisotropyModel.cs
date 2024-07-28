using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Data.Profiles;
using userspace_backend.Model.EditableSettings;

namespace userspace_backend.Model.ProfileComponents
{
    public class AnisotropyModel : EditableSettingsCollection<Anisotropy>
    {
        public AnisotropyModel(Anisotropy dataObject) : base(dataObject)
        {
        }

        public EditableSetting<double> DomainX { get; set; }

        public EditableSetting<double> DomainY { get; set; }

        public EditableSetting<double> RangeX { get; set; }

        public EditableSetting<double> RangeY { get; set; }

        public EditableSetting<double> LPNorm { get; set; }

        public override Anisotropy MapToData()
        {
            return new Anisotropy()
            {
                Domain = new Vector2() { X = DomainX.ModelValue, Y = DomainY.ModelValue },
                Range = new Vector2() { X = RangeX.ModelValue, Y = RangeY.ModelValue },
                LPNorm = LPNorm.ModelValue,
            };
        }

        protected override IEnumerable<IEditableSetting> EnumerateEditableSettings()
        {
            return [DomainX, DomainY, RangeX, RangeY, LPNorm];
        }

        protected override IEnumerable<IEditableSettingsCollection> EnumerateEditableSettingsCollections()
        {
            return Enumerable.Empty<IEditableSettingsCollection>();
        }

        protected override void InitEditableSettingsAndCollections(Anisotropy dataObject)
        {
            DomainX = new EditableSetting<double>(dataObject.Domain.X, UserInputParsers.DoubleParser);
            DomainY = new EditableSetting<double>(dataObject.Domain.Y, UserInputParsers.DoubleParser);
            RangeX = new EditableSetting<double>(dataObject.Range.X, UserInputParsers.DoubleParser);
            RangeY = new EditableSetting<double>(dataObject.Range.Y, UserInputParsers.DoubleParser);
            LPNorm = new EditableSetting<double>(dataObject.LPNorm, UserInputParsers.DoubleParser);
        }
    }
}
