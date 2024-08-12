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
using System.Collections.ObjectModel;

namespace userspace_backend.Model
{
    public class MappingModel : EditableSettingsCollection<DATA.Mapping>
    {
        public MappingModel(
            Mapping dataObject,
            IModelValueValidator<string> nameValidator,
            DeviceGroups deviceGroups,
            ProfilesModel profiles) : base(dataObject)
        {
            NameValidator = nameValidator;
            SetActive = true;
            DeviceGroups = deviceGroups;
            Profiles = profiles;
            InitIndividualMappings(dataObject);
        }

        public bool SetActive { get; set; }

        public EditableSetting<string> Name { get; set; }

        public ObservableCollection<MappingGroup> IndividualMappings { get; protected set; }

        public ObservableCollection<DeviceGroups> DeviceGroupsStillUnmapped { get; protected set; }
        
        protected IModelValueValidator<string> NameValidator { get; }

        protected DeviceGroups DeviceGroups { get; }

        protected ProfilesModel Profiles { get; }

        public override Mapping MapToData()
        {
            Mapping mapping = new Mapping();

            foreach (var group in IndividualMappings)
            {
                mapping.GroupsToProfiles.Add(group.DeviceGroup.ModelValue, group.Profile.Name.ModelValue);
            }

            return mapping;
        }

        protected override IEnumerable<IEditableSetting> EnumerateEditableSettings()
        {
            return [];
        }

        protected override IEnumerable<IEditableSettingsCollection> EnumerateEditableSettingsCollections()
        {
            return [];
        }

        protected override void InitEditableSettingsAndCollections(Mapping dataObject)
        {
            Name = new EditableSetting<string>(
                displayName: "Name",
                initialValue: dataObject.Name,
                parser: UserInputParsers.StringParser,
                validator: NameValidator);

            IndividualMappings = new ObservableCollection<MappingGroup>();
        }

        protected void InitIndividualMappings(Mapping dataObject)
        {
            foreach (var kvp in dataObject.GroupsToProfiles)
            {
                TryAddMapping(kvp.Key, kvp.Value);
            }
        }

        public bool TryAddMapping(string deviceGroupName, string profileName)
        {
            if (!DeviceGroups.TryGetDeviceGroup(deviceGroupName, out DeviceGroupModel? deviceGroup)
                || deviceGroup == null
                || IndividualMappings.Any(m => m.DeviceGroup.Equals(deviceGroup)))
            {
                return false;
            }

            if (!Profiles.TryGetProfile(profileName, out ProfileModel? profile)
                || profile == null)
            {
                return false;
            }

            MappingGroup group = new MappingGroup()
            {
                DeviceGroup = deviceGroup,
                Profile = profile,
            };

            IndividualMappings.Add(group);
            return true;
        }
    }

    public class MappingGroup
    {
        public DeviceGroupModel DeviceGroup { get; set; }

        public ProfileModel Profile { get; set; }
    }
}
