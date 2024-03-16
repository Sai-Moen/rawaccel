using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace userspace_backend.Model
{
    public class EditableSetting<T> : IEditableSetting where T : IEquatable<T>
    {
        public EditableSetting(T value)
        {
            EditableValue = value;
            LastKnownValue = value;
        }

        public T EditableValue { get; set; }

        public T LastKnownValue { get; protected set; }

        public bool HasChanged() => !EditableValue.Equals(LastKnownValue);
    }
}
