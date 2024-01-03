using ReactiveUI;
using userinterface.Models.Charts;
using userinterface.Models.Mouse;

namespace userinterface.ViewModels;

public sealed class ChartViewModel : ViewModelBase, IMouseMoveDisplayer
{
    private Chart chart = new();

    public ChartViewModel(double initWidth, double initHeight)
    {
        Width = initWidth;
        Height = initHeight;
    }

    public double Width
    {
        get => chart.Width;
        set => chart.Width = value;
    }

    public double Height
    {
        get => chart.Height;
        set => chart.Height = value;
    }

    #region LastMouseMove

    private double lmmx;
    public double LMMX
    {
        get => lmmx;
        set => this.RaiseAndSetIfChanged(ref lmmx, value);
    }

    private double lmmy;
    public double LMMY
    {
        get => lmmy;
        set => this.RaiseAndSetIfChanged(ref lmmy, value);
    }

    public void SetLastMouseMove(float x, float y)
    {
        LMMX = x;
        LMMY = y;
    }

    #endregion LastMouseMove
}
