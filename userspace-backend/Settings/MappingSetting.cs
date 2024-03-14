using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Data;

namespace userspace_backend.Settings
{
    public class MappingSetting
    {
        public string Name { get; set; }

        protected string LoadedName { get; set; }

        public Mapping Mapping { get; set; }

        protected Mapping LoadedMapping { get; set; }
        
        public bool HasChanged()
        {
            return Name != LoadedName || Mapping != LoadedMapping;
        }
    }
}
