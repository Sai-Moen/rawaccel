using System;
using System.Collections.Generic;
using userspace_backend.Data.Profiles;
using userspace_backend.Data.Profiles.Accel;
using userspace_backend.Model.EditableSettings;
using userspace_backend.Model.ProfileComponents;
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

        public AnisotropyModel Anisotropy { get; set; }

        public CoalescionModel Coalescion { get; set; }

        public FormulaAccelModel FormulaAccel { get => DefinitionModels[AccelerationDefinitionType.Formula] as FormulaAccelModel; }

        public override Acceleration MapToData()
        {
            return DefinitionModels[DefinitionType.ModelValue].MapToData();
        }

        public AccelArgs MapToDriver()
        {
            return DefinitionModels[DefinitionType.ModelValue].MapToDriver();
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
                initialValue: dataObject?.Type ?? AccelerationDefinitionType.None,
                parser: UserInputParsers.AccelerationDefinitionTypeParser,
                validator: ModelValueValidators.DefaultAccelerationTypeValidator,
                // When the definition type changes, contained editable settings collections need to correspond to new type
                setCallback: GatherEditableSettingsCollections);

            DefinitionModels = new Dictionary<AccelerationDefinitionType, IAccelDefinitionModel>();
            foreach (AccelerationDefinitionType defnType in Enum.GetValues(typeof(AccelerationDefinitionType)))
            {
                DefinitionModels.Add(defnType, CreateAccelerationDefinitionModelOfType(defnType, dataObject));
            }

            Anisotropy = new AnisotropyModel(dataObject?.Anisotropy);
            Coalescion = new CoalescionModel(dataObject?.Coalescion);
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
