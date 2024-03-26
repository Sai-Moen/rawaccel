using Avalonia.Input;
using Avalonia.Interactivity;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace userinterface.ViewModels
{
    public class EditableFieldViewModel : ViewModelBase
    {
        private string _name;

        public EditableFieldViewModel(string name, string initialValue)
        {
            _name = name;
            ValueText = initialValue;
        }

        public string Name { get => $"{_name}: "; }

        public string ValueText { get; set; }

        public void TakeValueTextAsNewValue()
        {
        }
    }
}
