using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels
{
    public partial class MappingsPageViewModel : ViewModelBase
    {
        public MappingsPageViewModel(BE.MappingsModel mappingsBE)
        {
            MappingsBE = mappingsBE;
            MappingViews = new ObservableCollection<MappingViewHolder>();
            UpdateMappingViews();
            MappingsBE.Mappings.CollectionChanged += MappingsCollectionChanged;
        }

        public BE.MappingsModel MappingsBE { get; }

        public ObservableCollection<MappingViewHolder> MappingViews { get; }

        public void UpdateMappingViews()
        {
            MappingViews.Clear();

            foreach(BE.MappingModel mappingBE in MappingsBE.Mappings)
            {
                MappingViews.Add(new MappingViewHolder(mappingBE));
            }
        }

        private void MappingsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateMappingViews();
        }
    }

    public class MappingViewHolder
    {
        public MappingViewHolder(BE.MappingModel mapping)
        {
            MappingView = new MappingViewModel(mapping);
        }

        public MappingViewModel MappingView { get; }
    }
}
