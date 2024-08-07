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
    public class MappingConstructor
    {
        public MappingConstructor(ProfilesModel profiles, DeviceGroups deviceGroups)
        {
            Profiles = profiles;
            DeviceGroups = deviceGroups;
        }

        public ProfilesModel Profiles { get; }

        public DeviceGroups DeviceGroups { get; }
    }
}
