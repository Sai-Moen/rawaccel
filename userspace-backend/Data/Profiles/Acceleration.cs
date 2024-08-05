using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public virtual AccelerationDefinitionType Type { get; init; }

        public Anisotropy Anisotropy { get; init; }
    }
}
