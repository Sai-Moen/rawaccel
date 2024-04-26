using Avalonia.Input;
using Avalonia.Interactivity;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Model.EditableSettings;

namespace userinterface.ViewModels
{
    public class EditableFieldViewModel<T> : ViewModelBase where T : IEquatable<T>
    {
        private string _name;

        public EditableFieldViewModel(string name, EditableSetting<T> editableSetting)
        {
            _name = name;
            EditableSetting = editableSetting;
            SetValueTextFromEditableSetting();
        }

        public string Name { get => $"{_name}: "; }

        public string ValueText { get; set; }

        public EditableSetting<T> EditableSetting { get; }
 
        public void TakeValueTextAsNewValue()
        {
            if (!EditableSetting.TryParseAndSet(ValueText))
            {
                //TODO throw new exception here
            }

            SetValueTextFromEditableSetting();
        }

        protected void SetValueTextFromEditableSetting()
        {
            ValueText = EditableSetting.EditableValue?.ToString() ?? string.Empty;
        }
    }
}
