using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace userspace_backend.Data.Profiles
{
    public class Hidden
    {
        public double RotationDegrees { get; set; }

        public double AngleSnappingDegrees { get; set; }

        public double LeftRightRatio { get; set; }

        public double UpDownRatio { get; set; }

        public double SpeedCap { get; set; }
    }
}
