using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace userspace_backend.Data.Profiles.Accel.Formula
{
    public class PowerAccel : FormulaAccel
    {
        public override AccelerationFormulaType FormulaType => AccelerationFormulaType.Power;

        public double Scale { get; set; }

        public double Exponent { get; set; }

        public double OutputOffset { get; set; }

        public double Cap { get; set; }
    }
}
