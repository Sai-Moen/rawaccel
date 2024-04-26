using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace userspace_backend.Model.EditableSettings
{
    public class EditableSetting<T> : IEditableSetting where T : IEquatable<T>
    {
        public EditableSetting(T initialValue, IParser<T> parser)
        {
            EditableValue = initialValue;
            LastKnownValue = initialValue;
            Parser = parser;
        }

        public T EditableValue { get; set; }

        public T LastKnownValue { get; protected set; }

        private IParser<T> Parser { get; }

        public bool HasChanged() => !EditableValue.Equals(LastKnownValue);

        public bool TryParseAndSet(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            if (Parser.TryParse(input.Trim(), out T parsedValue))
            {
                EditableValue = parsedValue;
                return true;
            }

            return false;
        }
    }
}
