using System;
using System.Collections.Generic;
using System.Text;

namespace userinterface.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel()
        {
            Profiles = new ProfilesViewModel();
        }

        public ProfilesViewModel Profiles { get; }

        public string Test => "Is this working?";
    }
}
