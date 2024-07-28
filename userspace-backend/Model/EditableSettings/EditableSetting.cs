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
            T initialValue,
            IUserInputParser<T> parser,
            Action setCallback = null)
        {
            LastWrittenValue = initialValue;
            Parser = parser;
            SetCallback = setCallback;
            UpdateModelValue();
            UpdateInterfaceValue();
        }

        /// <summary>
        /// This value can be bound in UI
        /// </summary>
        public string InterfaceValue { get; protected set; }

        public T ModelValue { get; protected set; }

        public T LastWrittenValue { get; protected set; }

        public string EditedValueForDiplay { get => ModelValue?.ToString() ?? string.Empty; }

        protected Action SetCallback { get; }

        private IUserInputParser<T> Parser { get; }

        public bool HasChanged() => ModelValue.CompareTo(LastWrittenValue) == 0;

        public bool TryUpdateFromInterface()
        {
            if (string.IsNullOrEmpty(InterfaceValue))
            {
                return false;
            }

            if (!Parser.TryParse(InterfaceValue.Trim(), out T parsedValue))
            {
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
