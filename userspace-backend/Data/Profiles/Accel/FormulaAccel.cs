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
            Linear,
            Classic,
            Power,
            Natural,
            Jump,
            Motivity,
        }

        public override AccelerationDefinitionType Type { get => AccelerationDefinitionType.Formula; }

        public virtual AccelerationFormulaType FormulaType { get; }

        public bool Gain { get; set; }
    }
}
