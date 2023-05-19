using System.Text.Json.Serialization;
using System.Windows;

namespace userinterface.Models.Settings
{
    public class DirectionalParams
    {
        #region Constructors

        public DirectionalParams(
            string mode,
            bool applyAsSens,
            double ioffset,
            double ooffset,
            double accel,
            double decayRate,
            double growthRate,
            double motivity,
            double exponentClassic,
            double scale,
            double exponentPower,
            double limit,
            double midpoint,
            double smooth,
            double capX,
            double capY,
            string capMode,
            double[] data)
        {
            Mode = mode;
            ApplyAsSensitivity = applyAsSens;
            InputOffset = ioffset;
            OutputOffset = ooffset;
            Acceleration = accel;
            DecayRate = decayRate;
            GrowthRate = growthRate;
            Motivity = motivity;
            ExponentClassic = exponentClassic;
            Scale = scale;
            ExponentPower = exponentPower;
            Limit = limit;
            Midpoint = midpoint;
            Smooth = smooth;
            CapOrJump = new Point(capX, capY);
            CapMode = capMode;
            Data = data;
        }

        #endregion Constructors

        #region Properties

        [JsonPropertyOrder(0)]
        public string Mode { get; set; }

        [JsonPropertyOrder(1)]
        public bool ApplyAsSensitivity { get; set; }

        [JsonPropertyOrder(2)]
        public double InputOffset { get; set; }

        [JsonPropertyOrder(3)]
        public double OutputOffset { get; set; }

        [JsonPropertyOrder(4)]
        public double Acceleration { get; set; }

        [JsonPropertyOrder(5)]
        public double DecayRate { get; set; }

        [JsonPropertyOrder(6)]
        public double GrowthRate { get; set; }

        [JsonPropertyOrder(7)]
        public double Motivity { get; set; }

        [JsonPropertyOrder(8)]
        public double ExponentClassic { get; set; }

        [JsonPropertyOrder(9)]
        public double Scale { get; set; }

        [JsonPropertyOrder(10)]
        public double ExponentPower { get; set; }

        [JsonPropertyOrder(11)]
        public double Limit { get; set; }

        [JsonPropertyOrder(12)]
        public double Midpoint { get; set; }

        [JsonPropertyOrder(13)]
        public double Smooth { get; set; }

        [JsonPropertyOrder(14)]
        public Point CapOrJump { get; set; }

        [JsonPropertyOrder(15)]
        public string CapMode { get; set; }

        [JsonPropertyOrder(16)]
        public double[] Data { get; set; }

        #endregion Properties
    }
}
