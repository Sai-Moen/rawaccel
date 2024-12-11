using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using BE = userspace_backend.Model.AccelDefinitions;
using BEData = userspace_backend.Data.Profiles.Acceleration;

namespace userinterface.ViewModels
{
    public partial class AccelerationProfileSettingsViewModel : ViewModelBase
    {
        public static ObservableCollection<BEData.AccelerationDefinitionType> DefinitionTypes =
            new ObservableCollection<BEData.AccelerationDefinitionType>(
                Enum.GetValues(typeof(BEData.AccelerationDefinitionType)).Cast<BEData.AccelerationDefinitionType>());

        [ObservableProperty]
        public bool areSpecificSettingsVisible;

        public AccelerationProfileSettingsViewModel(BE.AccelerationModel accelerationBE)
        {
            AccelerationBE = accelerationBE;
            AnisotropySettings = new AnisotropyProfileSettingsViewModel(accelerationBE.Anisotropy);
            CoalescionSettings = new CoalescionProfileSettingsViewModel(accelerationBE.Coalescion);
        }

        public BE.AccelerationModel AccelerationBE { get; }

        public ObservableCollection<BEData.AccelerationDefinitionType> DefinitionTypesLocal => DefinitionTypes;

        public AnisotropyProfileSettingsViewModel AnisotropySettings { get; set; }

        public CoalescionProfileSettingsViewModel CoalescionSettings { get; set; }

        public void UpdateAreSpecificSettingsVisible()
        {
            AreSpecificSettingsVisible = AccelerationBE.DefinitionType.ModelValue != BEData.AccelerationDefinitionType.None;
        }
    }
}
