using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DATA = userspace_backend.Data;
using userspace_backend.Model.AccelDefinitions;
using userspace_backend.Model.EditableSettings;
using userspace_backend.Model.ProfileComponents;
using userspace_backend.Data;
using CommunityToolkit.Mvvm.Collections;

namespace userspace_backend.Model
{
    public class MappingModel : EditableSettingsCollection<DATA.Mapping>
    {
        public MappingModel(
            Mapping dataObject,
            IModelValueValidator<string> nameValidator) : base(dataObject)
        {
            NameValidator = nameValidator;
        }

        public EditableSetting<string> Name { get; set; }

        public ObservableGroupedCollection<string, MappingGroup> IndividualMappings { get; protected set; }

        protected IModelValueValidator<string> NameValidator { get; }

        public override Mapping MapToData()
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<IEditableSetting> EnumerateEditableSettings()
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<IEditableSettingsCollection> EnumerateEditableSettingsCollections()
        {
            throw new NotImplementedException();
        }

        protected override void InitEditableSettingsAndCollections(Mapping dataObject)
        {
            Name = new EditableSetting<string>(
                displayName: "Name",
                initialValue: dataObject.Name,
                parser: UserInputParsers.StringParser,
                validator: NameValidator);

            IndividualMappings = new ObservableGroupedCollection<string, MappingGroup>();

            foreach (var kvp in dataObject.GroupsToProfiles)
            {
            }
        }
    }

    public class MappingGroup
    {
        public DeviceGroupModel DeviceGroup { get; set; }

        public ProfileModel Profile { get; set; }
}
