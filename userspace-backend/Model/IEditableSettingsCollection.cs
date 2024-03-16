using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace userspace_backend.Model
{
    public interface IEditableSettingsCollection
    {
        bool HasChanged { get; }
    }
}
