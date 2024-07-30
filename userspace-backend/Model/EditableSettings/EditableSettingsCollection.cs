using System;
using System.Collections.Generic;
using System.Linq;

namespace userspace_backend.Model.EditableSettings
{
    public abstract class EditableSettingsCollection<T> : IEditableSettingsCollection
    {
        public EditableSettingsCollection(T dataObject)
        {
            InitEditableSettingsAndCollections(dataObject);
            GatherEditableSettings();
            GatherEditableSettingsCollections();
        }

        public IEnumerable<IEditableSetting> AllContainedEditableSettings { get; set; }

        public IEnumerable<IEditableSettingsCollection> AllContainedEditableSettingsCollections { get; set; }

        public bool HasChanged { get; protected set; }

        public void EvaluateWhetherHasChanged()
        {
            if (AllContainedEditableSettings.Any(s => s.HasChanged()) ||
                AllContainedEditableSettingsCollections.Any(c => c.HasChanged))
            {
                HasChanged = true;
            }
            else
            {
                HasChanged = false;
            }
        }

        public void GatherEditableSettings()
        {
            AllContainedEditableSettings = EnumerateEditableSettings();
        }
        public void GatherEditableSettingsCollections()
        {
            AllContainedEditableSettingsCollections = EnumerateEditableSettingsCollections();
        }

        protected abstract void InitEditableSettingsAndCollections(T dataObject);

        protected abstract IEnumerable<IEditableSetting> EnumerateEditableSettings();

        protected abstract IEnumerable<IEditableSettingsCollection> EnumerateEditableSettingsCollections();

        public abstract T MapToData();
    }
}
