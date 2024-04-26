using System.Collections.Generic;
using System.Linq;

namespace userspace_backend.Model.EditableSettings
{
    public abstract class EditableSettingsCollection<T> : IEditableSettingsCollection
    {
        public EditableSettingsCollection(T dataObject)
        {
            InitEditableSettingsAndCollections(dataObject);
            EditableSettings = GetEditableSettings();
            EditableSettingsCollections = GetEditableSettingsCollections();
        }

        public IEnumerable<IEditableSetting> EditableSettings { get; set; }

        public IEnumerable<IEditableSettingsCollection> EditableSettingsCollections { get; set; }

        public bool HasChanged { get; protected set; }

        public void EvaluateWhetherHasChanged()
        {
            if (EditableSettings.Any(s => s.HasChanged()) ||
                EditableSettingsCollections.Any(c => c.HasChanged))
            {
                HasChanged = true;
            }
            else
            {
                HasChanged = false;
            }
        }

        protected abstract void InitEditableSettingsAndCollections(T dataObject);

        protected abstract IEnumerable<IEditableSetting> GetEditableSettings();

        protected abstract IEnumerable<IEditableSettingsCollection> GetEditableSettingsCollections();

        public abstract T MapToData();
    }
}
