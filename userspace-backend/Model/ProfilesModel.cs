using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DATA = userspace_backend.Data;
using userspace_backend.Model.EditableSettings;

namespace userspace_backend.Model
{
    public class ProfilesModel : EditableSettingsCollection<IEnumerable<DATA.Profile>>
    {
        public static readonly ProfileModel DefaultProfile = new ProfileModel(
            GenerateNewDefaultProfile("Default"), ModelValueValidators.AllChangesInvalidStringValidator);

        public ProfilesModel(IEnumerable<DATA.Profile> dataObject) : base(dataObject)
        {
            NameValidator = new ProfileNameValidator(this);
        }

        public ObservableCollection<ProfileModel> Profiles { get; protected set; }

        protected ProfileNameValidator NameValidator { get; }

        public override IEnumerable<DATA.Profile> MapToData()
        {
            return Profiles.Select(p => p.MapToData());
        }

        protected override IEnumerable<IEditableSetting> EnumerateEditableSettings()
        {
            return [];
        }

        protected override IEnumerable<IEditableSettingsCollection> EnumerateEditableSettingsCollections()
        {
            return Profiles;
        }

        protected override void InitEditableSettingsAndCollections(IEnumerable<DATA.Profile> dataObject)
        {
            Profiles = new ObservableCollection<ProfileModel>();
        }

        public bool TryGetProfile(string name, out ProfileModel? profileModel)
        {
            profileModel = Profiles.FirstOrDefault(
                p => string.Equals(p.Name.ModelValue, name, StringComparison.InvariantCultureIgnoreCase));

            return profileModel != null;
        }

        public bool TryAddNewDefaultProfile(string name)
        {
            if (TryGetProfile(name, out _))
            {
                return false;
            }

            DATA.Profile profile = GenerateNewDefaultProfile(name);
            ProfileModel profileModel = new ProfileModel(profile, NameValidator);
            Profiles.Add(profileModel);
            return true;
        }

        public bool TryAddProfile(DATA.Profile profileToAdd)
        {
            if (TryGetProfile(profileToAdd.Name, out _))
            {
                return false;
            }

            ProfileModel profileModel = new ProfileModel(profileToAdd, NameValidator);
            Profiles.Add(profileModel);
            return true;
        }

        protected static DATA.Profile GenerateNewDefaultProfile(string name)
        {
            return new DATA.Profile()
            {
                Name = name,
            };
        }
    }

    public class ProfileNameValidator(ProfilesModel profilesModel) : IModelValueValidator<string>
    {
        ProfilesModel ProfilesModel { get; } = profilesModel;

        public bool Validate(string value)
        {
            return !ProfilesModel.TryGetProfile(value, out _);
        }
    }
}
