using Avalonia;
using ReactiveUI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using userinterface.Models.Charts;
using userinterface.Models.Mouse;

namespace userinterface.ViewModels;

public sealed class ChartsViewModel : ViewModelBase, IMouseMoveDisplayer
{
    private double lmmx = 0;
    public double LMMX
    {
        get => lmmx;
        set => this.RaiseAndSetIfChanged(ref lmmx, value);
    }

    private double lmmy = 0;
    public double LMMY
    {
        get => lmmy;
        set => this.RaiseAndSetIfChanged(ref lmmy, value);
    }

    private Chart chart = new()
    {
        Title = "First Chart",

        Width = 600,
        Height = 400,

        Graph = new()
        {
            Width = 400,
            Height = 200,

            Points = chartPoints
        },

        AxisX = new()
        {
            Title = "X Axis",
        },

        AxisY = new()
        {
            Title = "Y Axis",
        },
    };
    public Chart Chart
    {
        get => chart;
        set => this.RaiseAndSetIfChanged(ref chart, value);
    }

    private static readonly IList<Point> chartPoints = new ObservableCollection<Point>()
    {
        new(0, 50),
        new(25, 25),
        new(50, 0),
        new(75, -50),
    };

    public void SetLastMouseMove(float x, float y)
    {
        LMMX = x;
        LMMY = y;
    }
}
