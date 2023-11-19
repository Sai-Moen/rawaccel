using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Media;
using userinterface.Models.Charts;

namespace userinterface.DataTemplates;

public static class ChartTemplateProvider
{
    public static FuncDataTemplate<Chart> ChartTemplate
        => new((chart) => chart is not null && chart.IsReady, ChartTemplateBuilder);

    private static Grid ChartTemplateBuilder(Chart chart)
    {
        Grid grid = new()
        {
            RowDefinitions = new("*,Auto,*"),
            ColumnDefinitions = new("*,Auto"),

            Width = chart.Width,
            Height = chart.Height,

            ShowGridLines = true, // Remove later
        };

        TextBlock title = new()
        {
            Text = chart.Title,

            [Grid.RowProperty] = 0,
            [Grid.ColumnProperty] = 1,
        };
        grid.Children.Add(title);

        grid.Children.Add(GraphTemplateBuilder(chart.Graph!));

        return grid;
    }

    private static Canvas GraphTemplateBuilder(Graph graph)
    {
        Canvas canvas = new()
        {
            Background = Brushes.White,
            Width = graph.Width,
            Height = graph.Height,

            [Grid.RowProperty] = 1,
            [Grid.ColumnProperty] = 1,
        };

        Polyline curve = new()
        {
            Stroke = Brushes.Green,
            Points = graph.Points,

            [Canvas.LeftProperty] = 0,
            [Canvas.BottomProperty] = 0,
        };
        canvas.Children.Add(curve);

        Ellipse lmm = new()
        {
            Width = 16,
            Height = 16,

            Fill = Brushes.Red,
        };
        canvas.Children.Add(lmm);

        return canvas;
    }
}
