using Avalonia.Controls;
using System.Linq;
using System.Collections.ObjectModel;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels
{
    public partial class MappingViewModel : ViewModelBase
    {
        public MappingViewModel(BE.MappingModel mappingBE)
        {
            MappingBE = mappingBE;
        }

        public BE.MappingModel MappingBE { get; }

        public ObservableCollection<BE.MappingGroup> IndividualMappings { get => MappingBE.IndividualMappings; }

        public void HandleAddMappingSelection(SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0
                && e.AddedItems[0] is BE.DeviceGroupModel deviceGroup)
            {
                MappingBE.TryAddMapping(deviceGroup.CurrentValidatedValue, BE.ProfilesModel.DefaultProfile.CurrentNameForDisplay);
            }
        }
    }
}
