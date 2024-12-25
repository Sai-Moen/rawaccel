using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace userspace_backend.Data.Profiles.Accel
{
    public class NoAcceleration : Acceleration
    {
        public override AccelerationDefinitionType Type { get => AccelerationDefinitionType.None; }
    }
}
