using System.Globalization;

namespace userinterface
{
    public static class Constants
    {
        #region Constants

        /// <summary> Version number string. </summary>
        public const string Version = "1.7.0";

        /// <summary> Relative Path of the Config file. </summary>
        public const string ConfigFileName = @".config";

        /// <summary> Relative Path of the Profile folder. </summary>
        public const string ProfileFolderName = @"Profiles/";

        /// <summary> Default profile name. </summary>
        public const string DefaultProfileName = "Default";

        /// <summary> DPI by which charts are scaled if none is set by user. </summary>
        public const int DefaultDPI = 1600;

        /// <summary> Poll rate by which charts are scaled if none is set by user. </summary>
        public const int DefaultPollRate = 1000;

        /// <summary> Resolution of chart calulation. </summary>
        public const int Resolution = 500;

        /// <summary> Multiplied by DPI over poll rate to find rough max expected velocity. </summary>
        public const double MaxMultiplier = .075;

        /// <summary> Number of divisions between 0 and 90 degrees for directional lookup. For 19: 0, 5, 10... 85, 90.</summary>
        public const int AngleDivisions = 19;

        /// <summary> Format string for gain cap active value label. </summary>
        public const string GainCapFormatString = "0.##";

        /// <summary> Format string for shortened x and y textboxes. </summary>
        public const string ShortenedFormatString = "0.###";

        /// <summary> Format string for shortened x and y fields. </summary>
        public const string ShortenedFieldFormatString = "0.###";

        /// <summary> Format string for default active value labels. </summary>
        public const string DefaultActiveValueFormatString = "0.######";

        /// <summary> Format string for default textboxes. </summary>
        public const string DefaultFieldFormatString = "0.#########";

        /// <summary> Format string for shortened x and y dropdowns. </summary>
        public const string AccelDropDownDefaultFullText = "Acceleration Type";

        /// <summary> Format string for default dropdowns. </summary>
        public const string AccelDropDownDefaultShortText = "Accel Type";

        /// <summary> Title of sensitivity chart. </summary>
        public const string SensitivityChartTitle = "Sensitivity";

        /// <summary> Title of velocity chart. </summary>
        public const string VelocityChartTitle = "Velocity";

        /// <summary> Title of gain chart. </summary>
        public const string GainChartTitle = "Gain";

        /// <summary> Text for x component. </summary>
        public const string XComponent = "X";

        /// <summary> Text for y component. </summary>
        public const string YComponent = "Y";

        /// <summary> Text to directionality panel title when panel is closed. </summary>
        public const string DirectionalityTitleClosed = "Anisotropy \u25BC";

        /// <summary> Text to directionality panel title when panel is open. </summary>
        public const string DirectionalityTitleOpen = "Anisotropy \u25B2";

        /// <summary> Style used by System.Double.Parse </summary>
        public const NumberStyles FloatStyle = NumberStyles.Float | NumberStyles.AllowThousands;

        #endregion Constants
    }
}
