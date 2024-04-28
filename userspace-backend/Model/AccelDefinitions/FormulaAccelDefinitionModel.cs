using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Data.Profiles;
using userspace_backend.Data.Profiles.Accel;
using userspace_backend.Model.EditableSettings;

namespace userspace_backend.Model.AccelDefinitions
{
    public class FormulaAccelDefinitionModel : AccelDefinitionModel<FormulaAccel>
    {
        public FormulaAccelDefinitionModel(Acceleration dataObject) : base(dataObject)
        {
        }

        public EditableSetting<FormulaAccel.AccelerationFormulaType> FormulaType { get; set; }

        protected Dictionary<FormulaAccel.AccelerationFormulaType, IAccelDefinitionModel> FormulaModels { get; set; }

        public override Acceleration MapToData()
        {
            return FormulaModels[FormulaType.EditableValue].MapToData();
        }

        protected override IEnumerable<IEditableSetting> EnumerateEditableSettings()
        {
            return [ FormulaType ];
        }

        protected override IEnumerable<IEditableSettingsCollection> EnumerateEditableSettingsCollections()
        {
            return [ FormulaModels[FormulaType.EditableValue] ];
        }

        protected override FormulaAccel GenerateDefaultDataObject()
        {
            throw new NotImplementedException();
        }

        protected override void InitSpecificSettingsAndCollections(FormulaAccel dataObject)
        {
            throw new NotImplementedException();
        }
    }
}
