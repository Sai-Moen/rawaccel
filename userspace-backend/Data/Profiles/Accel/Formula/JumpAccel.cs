using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace userspace_backend.Data.Profiles.Accel.Formula
{
    public class JumpAccel : FormulaAccel

    {
        public override AccelerationFormulaType FormulaType => AccelerationFormulaType.Jump;

        public double Smooth { get; set; }

        public double Input { get; set; }

        public double Output { get; set; }
    }
}
