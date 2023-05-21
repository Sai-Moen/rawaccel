using System.Collections.Generic;

using RawAccel.Models.Settings;

namespace RawAccel.ViewModels
{
    public sealed class ProfilesViewModel : ViewModelBase
    {
        public ProfilesViewModel()
        {
            Profiles = new HashSet<Profile>
            {
                new Profile()
            };
        }

        public ISet<Profile> Profiles { get; set; }
    }
}
