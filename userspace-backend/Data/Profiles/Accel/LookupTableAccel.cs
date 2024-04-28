using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace userspace_backend.Data.Profiles.Accel
{
    public class LookupTableAccel : Acceleration
    {
        public enum LookupTableType
        {
            Velocity = 0,
            Sensitivity = 1,
        }

        public override AccelerationDefinitionType Type { get => AccelerationDefinitionType.LookupTable; }

        public LookupTableType ApplyAs { get; set; }

        public double[] Data { get; set; }
    }
}
