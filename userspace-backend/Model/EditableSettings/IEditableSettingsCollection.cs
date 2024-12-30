using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace userspace_backend.Model.EditableSettings
{
    public interface IEditableSettingsCollection
    {
        bool HasChanged { get; }

        EventHandler AnySettingChanged { get; set; }
    }
}
