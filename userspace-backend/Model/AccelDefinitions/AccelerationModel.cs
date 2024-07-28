using System;
using System.Collections.Generic;
using userspace_backend.Data.Profiles;
using userspace_backend.Data.Profiles.Accel;
using userspace_backend.Model.EditableSettings;
using static userspace_backend.Data.Profiles.Acceleration;

namespace userspace_backend.Model.AccelDefinitions
{
    public class AccelerationModel : EditableSettingsCollection<Acceleration>
    {
        public AccelerationModel(Acceleration dataObject) : base(dataObject)
        {
        }

        public EditableSetting<AccelerationDefinitionType> DefinitionType { get; set; }

        protected Dictionary<AccelerationDefinitionType, IAccelDefinitionModel> DefinitionModels { get; set; }

        public override Acceleration MapToData()
        {
            return DefinitionModels[DefinitionType.ModelValue].MapToData();
        }

        protected override IEnumerable<IEditableSetting> EnumerateEditableSettings()
        {
            return [DefinitionType];
        }

        protected override IEnumerable<IEditableSettingsCollection> EnumerateEditableSettingsCollections()
        {
            return [DefinitionModels[DefinitionType.ModelValue]];
        }

        protected override void InitEditableSettingsAndCollections(Acceleration dataObject)
        {
            DefinitionType = new EditableSetting<AccelerationDefinitionType>(
                displayName: "Definition Type",
                initialValue: dataObject.Type,
                parser: UserInputParsers.AccelerationDefinitionTypeParser,
                validator: ModelValueValidators.DefaultAccelerationTypeValidator,
                // When the definition type changes, contained editable settings collections need to correspond to new type
                setCallback: GatherEditableSettingsCollections);

            DefinitionModels = new Dictionary<AccelerationDefinitionType, IAccelDefinitionModel>();
            foreach (AccelerationDefinitionType defnType in Enum.GetValues(typeof(AccelerationDefinitionType)))
            {
                DefinitionModels.Add(defnType, CreateAccelerationDefinitionModelOfType(defnType, dataObject));
            }
        }

        protected IAccelDefinitionModel CreateAccelerationDefinitionModelOfType(AccelerationDefinitionType definitionType, Acceleration dataObject)
        {
            switch (definitionType)
            {
                case AccelerationDefinitionType.Formula:
                    return new FormulaAccelModel(dataObject);
                case AccelerationDefinitionType.LookupTable:
                    return new LookupTableDefinitionModel(dataObject);
                case AccelerationDefinitionType.None:
                default:
                    return new NoAccelDefinitionModel(dataObject);
            }
        }
    }
}
