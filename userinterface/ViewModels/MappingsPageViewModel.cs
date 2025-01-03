using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
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

        private void MappingsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateMappingViews();
        }

        public void UpdateMappingViews()
        {
            MappingViews.Clear();
            foreach(BE.MappingModel mappingBE in MappingsBE.Mappings)
            {
                MappingViews.Add(new MappingViewHolder(mappingBE, MappingsBE));
            }
        }

        public bool TryAddNewMapping()
        {
            return MappingsBE.TryAddMapping();
        }
    }

    public class MappingViewHolder
    {
        private readonly BE.MappingModel mapping;
        private readonly BE.MappingsModel mappings;

        public MappingViewHolder(BE.MappingModel mapping, BE.MappingsModel mappings)
        {
            this.mapping = mapping;
            this.mappings = mappings;
            MappingView = new MappingViewModel(mapping);
        }

        public MappingViewModel MappingView { get; }

        public void DeleteSelf()
        {
            bool success = mappings.RemoveMapping(mapping);
            Debug.Assert(success);
        }
    }
}
