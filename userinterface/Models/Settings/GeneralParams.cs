using System.Text.Json.Serialization;
using System.Windows;

namespace RawAccel.Models.Settings
{
    public class GeneralParams
    {
        #region Constructors

        public GeneralParams(
            double sensMult,
            double yxRatio,
            double lrRatio,
            double udRatio,
            double degreesRotation,
            double degreesAngleSnapping,
            double inputSpeedCap,
            bool whole,
            double lpNorm,
            double domainX,
            double domainY,
            double rangeX,
            double rangeY)
        {
            SensitivityMultiplier = sensMult;
            YXRatio = yxRatio;
            LRRatio = lrRatio;
            UDRatio = udRatio;
            DegreesRotation = degreesRotation;
            DegreesAngleSnapping = degreesAngleSnapping;
            InputSpeedCap = inputSpeedCap;
            WholeMode = whole;
            LpNorm = lpNorm;
            Domain = new Point(domainX, domainY);
            Range = new Point(rangeX, rangeY);
        }

        #endregion Constructors

        #region Properties

        [JsonPropertyOrder(0)]
        public double SensitivityMultiplier { get; set; }

        [JsonPropertyOrder(1)]
        public double YXRatio { get; set; }

        [JsonPropertyOrder(2)]
        public double LRRatio { get; set; }

        [JsonPropertyOrder(3)]
        public double UDRatio { get; set; }

        [JsonPropertyOrder(4)]
        public double DegreesRotation { get; set; }

        [JsonPropertyOrder(5)]
        public double DegreesAngleSnapping { get; set; }

        [JsonPropertyOrder(6)]
        public double InputSpeedCap { get; set; }

        [JsonPropertyOrder(7)]
        public bool WholeMode { get; set; }

        [JsonPropertyOrder(8)]
        public double LpNorm { get; set; }

        [JsonPropertyOrder(9)]
        public Point Domain { get; set; }

        [JsonPropertyOrder(10)]
        public Point Range { get; set; }

        #endregion Properties
    }
}
