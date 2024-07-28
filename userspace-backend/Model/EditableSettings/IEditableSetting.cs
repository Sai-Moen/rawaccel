using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace userspace_backend.Model.EditableSettings
{
    public interface IEditableSetting
    {
        string EditedValueForDiplay { get; }

        bool HasChanged();
    }
}
