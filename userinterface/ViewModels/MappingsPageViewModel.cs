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

        public void UpdateMappingViews()
        {
            MappingViews.Clear();

            foreach(BE.MappingModel mappingBE in MappingsBE.Mappings)
            {
                MappingViews.Add(new MappingViewHolder(this, mappingBE));
            }
        }

        public void DeleteMapping(BE.MappingModel mapping)
        {
            bool success = MappingsBE.Mappings.Remove(mapping);
            Debug.Assert(success);
        }

        private void MappingsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateMappingViews();
        }
    }

    public class MappingViewHolder
    {
        private readonly MappingsPageViewModel parent;
        private readonly BE.MappingModel mapping;

        public MappingViewHolder(MappingsPageViewModel parent, BE.MappingModel mapping)
        {
            this.parent = parent;
            this.mapping = mapping;
            MappingView = new MappingViewModel(mapping);
        }

        public MappingViewModel MappingView { get; }

        public void DeleteSelf()
        {
            parent.DeleteMapping(mapping);
        }
    }
}
