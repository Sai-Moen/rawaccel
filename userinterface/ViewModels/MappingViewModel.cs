using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }
}
