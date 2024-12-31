using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace userspace_backend.Display.Calculations
{
    public static class CurveCalculationHelpers
    {
        public const double SlowestHandSpeed = 0.05;
        public const double FastestHandSpeed = 200;
        public const double CurvePointsResolution = 256;

        public static ICollection<double> CalculateCurvePointSpeeds()
        {
            List<double> curvePointSpeeds = new List<double>();

            double ratio = FastestHandSpeed / SlowestHandSpeed;
            double sqrtRatio = Math.Sqrt(ratio);
            double middle = sqrtRatio * SlowestHandSpeed;
            double increment = 2.0 / (CurvePointsResolution - 1.0);

            for (double i = -1; i <= 1; i += increment)
            {
                double speed = middle * Math.Pow(sqrtRatio, i);
                curvePointSpeeds.Add(speed);
            }

            return curvePointSpeeds;
        }
    }
}
