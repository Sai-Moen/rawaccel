using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Media;
using ReactiveUI;
using userinterface.Models.Charts;

namespace userinterface.ViewModels;

public sealed class ChartsViewModel : ViewModelBase
{
    public static double FromLeft => 0;
    public static double FromBottom => 0;

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

    public Ellipse LastMouseMove { get; } = new()
    {
        Width = 8,
        Height = 8,

        Fill = Brushes.Red,
    };

    public void SetLastMouseMove(float x, float y)
    {
        LMMX = x;
        LMMY = y;
    }
}
