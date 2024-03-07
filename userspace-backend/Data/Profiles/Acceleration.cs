using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Data.Profiles.Accel;

namespace userspace_backend.Data.Profiles
{
    public class Acceleration
    {
        public enum AccelerationDefinitionType
        {
            None,
            Formula,
            LookupTable,
        }

        public AccelerationDefinitionType Type { get; set; }

        public AccelerationDefinition Definition { get; set; }
    }
}
