using System;
using Point = Avalonia.Point;

namespace userinterface.Models.Charts;

public class Axis
{
    public string? Title { get; init; }

    public double Width { get; init; }
    public double Height { get; init; }

    private Point minPosition;
    public Point MinPosition
    {
        get => minPosition;
        init => minPosition = ClampPoint(value);
    }

    private Point maxPosition;
    public Point MaxPosition
    {
        get => maxPosition;
        init => maxPosition = ClampPoint(value);
    }

    private const int TICKS = 8;

    public Point[] TickPositions
    {
        get
        {
            // lerp for now...
            Point[] positions = new Point[TICKS];
            for (int i = 0; i < TICKS; i++)
            {
                double x = (MinPosition.X + MaxPosition.X) / TICKS;
                double y = (MinPosition.Y + MaxPosition.Y) / TICKS;
                positions[i] = new(x, y);
            }
            return positions;
        }
    }

    private Point ClampPoint(Point point)
    {
        double x = Math.Clamp(point.X, 0, Width);
        double y = Math.Clamp(point.Y, 0, Height);
        return new(x, y);
    }
}
