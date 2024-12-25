using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace userspace_backend.Model.EditableSettings
{
    public interface IEditableSetting
    {
        string DisplayName { get; }

        string EditedValueForDiplay { get; }

        string InterfaceValue { get; set; }

        bool HasChanged();

        bool TryUpdateFromInterface();
    }
}
