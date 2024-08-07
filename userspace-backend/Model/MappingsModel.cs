using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DATA = userspace_backend.Data;
using userspace_backend.Model.EditableSettings;
using CommunityToolkit.Mvvm.ComponentModel;

namespace userspace_backend.Model
{
    public class MappingsModel : EditableSettingsCollection<DATA.MappingSet>
    {
        public MappingsModel(DATA.MappingSet dataObject, DeviceGroups deviceGroups, ProfilesModel profiles)
            : base(dataObject)
        {
            DeviceGroups = deviceGroups;
            Profiles = profiles;
            NameValidator = new MappingNameValidator(this);
        }

        public ObservableCollection<MappingModel> Mappings { get; protected set; }

        protected DeviceGroups DeviceGroups { get; }

        protected ProfilesModel Profiles { get; }

        protected MappingNameValidator NameValidator { get; }

        public MappingModel GetMappingToSetActive()
        {
            return Mappings.FirstOrDefault(m => m.SetActive);
        }

        public bool TryGetMapping(string name, out MappingModel? mapping)
        {
            mapping = Mappings.FirstOrDefault(
                m => string.Equals(m.Name.ModelValue, name, StringComparison.InvariantCultureIgnoreCase));

            return mapping != null;
        }

        public bool TryAddMapping(DATA.Mapping mappingToAdd)
        {
            if (TryGetMapping(mappingToAdd.Name, out _))
            {
                return false;
            }

            MappingModel mapping = new MappingModel(mappingToAdd, NameValidator);
            Mappings.Add(mapping);
            return true;
        }

        public override DATA.MappingSet MapToData()
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<IEditableSetting> EnumerateEditableSettings()
        {
            return [];
        }

        protected override IEnumerable<IEditableSettingsCollection> EnumerateEditableSettingsCollections()
        {
            return Mappings;
        }

        protected override void InitEditableSettingsAndCollections(DATA.MappingSet dataObject)
        {
            Mappings = new ObservableCollection<MappingModel>();

            foreach (DATA.Mapping mapping in dataObject?.Mappings ?? [])
            {
                TryAddMapping(mapping);
            }
        }
    }

    public class MappingNameValidator(MappingsModel mappings) : IModelValueValidator<string>
    {
        protected MappingsModel Mappings { get; } = mappings;
        public bool Validate(string value)
        {
            return !Mappings.TryGetMapping(value, out _);
        }
    }
}
