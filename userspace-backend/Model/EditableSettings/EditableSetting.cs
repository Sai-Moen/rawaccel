using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace userspace_backend.Model.EditableSettings
{
    public class EditableSetting<T> : IEditableSetting where T : IComparable
    {
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
            UpdateModelValue();
            UpdateInterfaceValue();
        }

        /// <summary>
        /// Display name for this setting in UI
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// This value can be bound in UI
        /// </summary>
        public string InterfaceValue { get; set; }

        public T ModelValue { get; protected set; }

        public T LastWrittenValue { get; protected set; }

        public string EditedValueForDiplay { get => ModelValue?.ToString() ?? string.Empty; }

        protected Action SetCallback { get; }

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

            ModelValue = parsedValue;
            SetCallback?.Invoke();
            return true;
        }

        protected void UpdateInterfaceValue()
        {
            InterfaceValue = ModelValue?.ToString();
        }

        protected void UpdateModelValue()
        {
            ModelValue = LastWrittenValue;
        }
    }
}
