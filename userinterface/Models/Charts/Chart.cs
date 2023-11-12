using Avalonia;

namespace userinterface.Models.Charts
{
    public class Chart
    {
        public static double OriginX => 0;
        public static double OriginY => 0;

        public string Title { get; }

        public Point[] Points { get; }

        public Chart(string title)
        {
            Title = title;
            Points = new Point[3] { new(1, 1), new(10, -10), new(200, -100) };
        }
    }
}
