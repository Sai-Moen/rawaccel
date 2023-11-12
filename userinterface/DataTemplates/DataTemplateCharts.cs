using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Media;
using userinterface.Models.Charts;

namespace userinterface.DataTemplates
{
    public static class DataTemplateCharts
    {
        public static FuncDataTemplate<Chart> ChartDataTemplate
            => new((chart) => chart is not null, BuildChartDataTemplate);

        private static Control BuildChartDataTemplate(Chart chart)
        {
            Canvas canvas = new()
            {
                Width = 400,
                Height = 200,
            };

            Polyline curve = new()
            {
                [!Canvas.LeftProperty] = new Binding("FromLeft"),
                [!Canvas.BottomProperty] = new Binding("FromBottom"),

                [!Polyline.PointsProperty] = new Binding(Chart.PathToPoints),

                Stroke = Brushes.Green,
            };
            canvas.Children.Add(curve);

            Ellipse lmm = new()
            {
                [!Canvas.LeftProperty] = new Binding("LMMX"),
                [!Canvas.BottomProperty] = new Binding("LMMY"),

                Width = 8,
                Height = 8,

                Fill = Brushes.Red,
            };
            canvas.Children.Add(lmm);

            return canvas;
        }
    }
}
