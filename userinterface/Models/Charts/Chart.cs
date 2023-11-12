using Avalonia;

namespace userinterface.Models.Charts
{
    public class Chart
    {
        public string Title { get; }

        public Point[] Points { get; }
        public static string PathToPoints => "Points";

        public Chart(string title)
        {
            Title = title;
            Points = new Point[3] { new(1, 1), new(10, 10), new(200, 100) };
        }
    }
}
