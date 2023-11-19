using System;
using System.Collections.Generic;
using Point = Avalonia.Point;

namespace userinterface.Models.Charts;

public class Graph
{
    private IList<Point> points = Array.Empty<Point>();
    public IList<Point> Points
    {
        get
        {
            List<Point> bounded = new(points.Count);
            foreach (Point point in points)
            {
                if (IsInBounds(point))
                {
                    bounded.Add(point);
                }
            }
            return bounded.ToArray();
        }
        set => points = value;
    }

    public double Width { get; init; }
    public double Height { get; init; }

    private bool IsInBounds(Point point)
    {
        return point.X >= 0 && point.Y >= 0 && point.X <= Width && point.Y <= Height;
    }
}
