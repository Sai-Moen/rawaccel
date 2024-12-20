using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        public bool areFormulaSettingsVisible;

        public AccelerationProfileSettingsViewModel(BE.AccelerationModel accelerationBE)
        {
            AccelerationBE = accelerationBE;
            AccelerationFormulaSettings = new AccelerationFormulaSettingsViewModel(accelerationBE.FormulaAccel);
            AnisotropySettings = new AnisotropyProfileSettingsViewModel(accelerationBE.Anisotropy);
            CoalescionSettings = new CoalescionProfileSettingsViewModel(accelerationBE.Coalescion);
            AccelerationBE.DefinitionType.AutoUpdateFromInterface = true;
            AccelerationBE.DefinitionType.PropertyChanged += OnDefinitionTypeChanged;
        }

        public BE.AccelerationModel AccelerationBE { get; }

        public ObservableCollection<BEData.AccelerationDefinitionType> DefinitionTypesLocal => DefinitionTypes;

        public AccelerationFormulaSettingsViewModel AccelerationFormulaSettings { get; }

        public AnisotropyProfileSettingsViewModel AnisotropySettings { get; set; }

        public CoalescionProfileSettingsViewModel CoalescionSettings { get; set; }

        private void OnDefinitionTypeChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AccelerationBE.DefinitionType.CurrentValidatedValue))
            {
                AreFormulaSettingsVisible = AccelerationBE.DefinitionType.ModelValue != BEData.AccelerationDefinitionType.None;
            }
        }

    }
}
