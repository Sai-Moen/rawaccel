using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Data;

namespace userspace_backend.Settings
{
    public class ProfileSetting
    {
        public string Name { get; set; }

        protected string LoadedName { get; set; }

        public Profile Profile { get; set; }

        protected Profile LoadedProfile { get; set; }

        public bool HasChanged()
        {
            return Name != LoadedName || Profile != LoadedProfile;
        }
    }
}
