using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using userspace_backend.Model;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels
{
    public partial class ProfileListViewModel : ViewModelBase
    {
        [ObservableProperty]
        public BE.ProfileModel currentSelectedProfile;

        private readonly BE.ProfilesModel profilesModel;

        public ProfileListViewModel(BE.ProfilesModel profiles, Action selectionChangeAction)
        {
            profilesModel = profiles;
            SelectionChangeAction = selectionChangeAction;
        }

        public ObservableCollection<BE.ProfileModel> Profiles => profilesModel.Profiles;

        public Action SelectionChangeAction { get; }

        public void RemoveSelectedProfile()
        {
            // pressing delete multiple times without re-selecting just does nothing
            _ = profilesModel.RemoveProfile(CurrentSelectedProfile);
        }

        partial void OnCurrentSelectedProfileChanged(ProfileModel value)
        {
            SelectionChangeAction.Invoke();
        }
    }
}
