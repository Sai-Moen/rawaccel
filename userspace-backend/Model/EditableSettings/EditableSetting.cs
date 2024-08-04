using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace userspace_backend.Model.EditableSettings
{
    public partial class EditableSetting<T> : ObservableObject, IEditableSetting where T : IComparable
    {
        /// <summary>
        /// This value can be bound in UI for direct editing
        /// </summary>
        [ObservableProperty]
        public string interfaceValue;

        /// <summary>
        /// This value can be bound in UI for readonly display of validated input
        /// </summary>
        [ObservableProperty]
        public string currentValidatedValue;

        public EditableSetting(
            string displayName,
            T initialValue,
            IUserInputParser<T> parser,
            IModelValueValidator<T> validator,
            Action setCallback = null)
        {
            DisplayName = displayName;
            LastWrittenValue = initialValue;
            Parser = parser;
            SetCallback = setCallback;
            Validator = validator;
            UpdateModelValueFromLastKnown();
            UpdateInterfaceValue();
        }

        /// <summary>
        /// Display name for this setting in UI
        /// </summary>
        public string DisplayName { get; }

        public virtual T ModelValue { get; protected set; }

        public T LastWrittenValue { get; protected set; }

        public string EditedValueForDiplay { get => ModelValue?.ToString() ?? string.Empty; }

        protected Action? SetCallback { get; }

        private IUserInputParser<T> Parser { get; }

        //TODO: change settings collections init so that this can be made private for non-static validators
        public IModelValueValidator<T> Validator { get; set; }

        public bool HasChanged() => ModelValue.CompareTo(LastWrittenValue) == 0;

        public bool TryUpdateFromInterface()
        {
            if (string.IsNullOrEmpty(InterfaceValue))
            {
                UpdateInterfaceValue();
                return false;
            }

            if (!Parser.TryParse(InterfaceValue.Trim(), out T parsedValue))
            {
                UpdateInterfaceValue();
                return false;
            }

            if (parsedValue.CompareTo(ModelValue) == 0)
            {
                return true;
            }

            if (!Validator.Validate(parsedValue))
            {
                UpdateInterfaceValue();
                return false;
            }

            UpdatedModeValue(parsedValue);
            SetCallback?.Invoke();
            return true;
        }

        protected void UpdateInterfaceValue()
        {
            InterfaceValue = ModelValue?.ToString();
        }

        protected void UpdateModelValueFromLastKnown()
        {
            UpdatedModeValue(LastWrittenValue);
        }

        protected void UpdatedModeValue(T value)
        {
            ModelValue = value;
            CurrentValidatedValue = ModelValue?.ToString();
        }

        partial void OnCurrentValidatedValueChanging(string? oldValue, string newValue)
        {
            newValue = oldValue ?? string.Empty;
        }
    }
}
