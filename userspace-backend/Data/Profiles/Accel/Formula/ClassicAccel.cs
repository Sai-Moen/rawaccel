using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace userspace_backend.Data.Profiles.Accel.Formula
{
    public class ClassicAccel : FormulaAccel
    {
        public override AccelFormulaType FormulaType => AccelFormulaType.Classic;

        public double Acceleration { get; set; }

        public double Exponent { get; set; }
    }
}
