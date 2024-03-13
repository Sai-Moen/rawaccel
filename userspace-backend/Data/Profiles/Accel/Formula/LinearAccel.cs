using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace userspace_backend.Data.Profiles.Accel.Formula
{
    public class LinearAccel : FormulaAccel
    {
        public override AccelFormulaType FormulaType => AccelFormulaType.Linear;

        public double Acceleration { get; set; }
    }
}
