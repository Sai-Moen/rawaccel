namespace RawAccel
{
    public static class Config
    {
        #region Constants

        /// <summary> DPI by which charts are scaled if none is set by user. </summary>
        public const int DefaultDPI = 1600;

        /// <summary> Poll rate by which charts are scaled if none is set by user. </summary>
        public const int DefaultPollRate = 1000;

        /// <summary> Toggles Last Mouse Move indicator. </summary>
        public const bool ShowLastMouseMove = true;

        /// <summary> Toggles Velocity and Gain charts. </summary>
        public const bool ShowVelocityAndGain = false;

        /// <summary> Toggles whether to write profile to driver upon starting the UI </summary>
        public const bool AutoWriteToDriverOnStartup = true;

        /// <summary> Default Theme Name </summary>
        public const string DefaultTheme = "Light Theme";

        #endregion Constants
    }
}
