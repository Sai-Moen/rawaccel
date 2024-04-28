using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace userspace_backend.Data.Profiles.Accel.Formula
{
    public class NaturalAccel : FormulaAccel
    {
        public override AccelerationFormulaType FormulaType => AccelerationFormulaType.Natural;

        public double DecayRate { get; set; }

        public double InputOffset { get; set; }

        public double Limit { get; set; }
    }
}
