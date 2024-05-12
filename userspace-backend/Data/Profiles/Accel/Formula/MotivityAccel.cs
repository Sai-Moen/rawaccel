using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace userspace_backend.Data.Profiles.Accel.Formula
{
    public class MotivityAccel : FormulaAccel
    {
        public override AccelerationFormulaType FormulaType => AccelerationFormulaType.Motivity;

        public double Motivity { get; set; }

        public double Midpoint { get; set; }

        public double GrowthRate { get; set; }
    }
}
