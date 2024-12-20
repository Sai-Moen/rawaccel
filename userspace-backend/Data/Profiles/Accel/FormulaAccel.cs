using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace userspace_backend.Data.Profiles.Accel
{
    public class FormulaAccel : Acceleration
    {
        public enum AccelerationFormulaType
        {
            Synchronous = 0,
            Linear = 1,
            Classic = 2,
            Power = 3,
            Natural = 4,
            Jump = 5,
        }

        public override AccelerationDefinitionType Type { get => AccelerationDefinitionType.Formula; }

        public virtual AccelerationFormulaType FormulaType { get; }

        public bool Gain { get; set; }
    }
}
