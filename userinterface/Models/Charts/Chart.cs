using Avalonia;

namespace userinterface.Models.Charts;

public enum ChartKind
{
    None,

    WholeSensitivity,
    WholeGain,
    WholeVelocity,

    XSensitivity,
    XGain,
    XVelocity,

    YSensitivity,
    YGain,
    YVelocity,
}

public class Chart
{
    public static double OriginX => 0;
    public static double OriginY => 0;

    public string? Title { get; init; }

    public Point[]? Points { get; init; }
}
