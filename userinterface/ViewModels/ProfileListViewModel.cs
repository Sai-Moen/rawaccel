using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using userspace_backend.Model;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels
{
    public partial class ProfileListViewModel : ViewModelBase
    {
        [ObservableProperty]
        public BE.ProfileModel currentSelectedProfile;

        public ProfileListViewModel(IEnumerable<BE.ProfileModel> profilesBE, Action selectionChangeAction)
        {
            Profiles = new ObservableCollection<BE.ProfileModel>(profilesBE);
            SelectionChangeAction = selectionChangeAction;
        }

        public ObservableCollection<BE.ProfileModel> Profiles { get; }

        public Action SelectionChangeAction { get; }

        partial void OnCurrentSelectedProfileChanged(ProfileModel value)
        {
            SelectionChangeAction.Invoke();
        }
    }
}
