using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace userspace_backend.Data.Profiles.Accel.Formula
{
    public class LinearAccel : FormulaAccel
    {
        public override AccelerationFormulaType FormulaType => AccelerationFormulaType.Linear;

        public double Acceleration { get; set; }

        public double Offset { get; set; }

        public double Cap { get; set; }
    }
}
